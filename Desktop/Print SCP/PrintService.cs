// Copyright (c) 2012-2021 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FellowOakDicom.Imaging.Codec;
using FellowOakDicom.Log;
using FellowOakDicom.Network;
using FellowOakDicom.Printing;

namespace FellowOakDicom.Samples.Printing
{
    public class PrintService : DicomService, IDicomServiceProvider, IDicomNServiceProvider, IDicomCEchoProvider
    {
        #region Properties and Attributes

        private static readonly DicomTransferSyntax[] _acceptedTransferSyntaxes = new DicomTransferSyntax[]
        {
           DicomTransferSyntax.ExplicitVRLittleEndian,
           DicomTransferSyntax.ExplicitVRBigEndian,
           DicomTransferSyntax.ImplicitVRLittleEndian
        };

        private static IDicomServer _server;

        public static Printer Printer { get; private set; }

        public string CallingAE { get; protected set; }
        public string CalledAE { get; protected set; }

        private FilmSession _filmSession;

        private readonly Dictionary<string, PrintJob> _printJobList = new Dictionary<string, PrintJob>();

        private bool _sendEventReports = false;

        private readonly object _synchRoot = new object();

        #endregion

        #region Constructors and Initialization

        public PrintService(INetworkStream stream, Encoding fallbackEncoding, ILogger log, ILogManager logmanager, INetworkManager network, ITranscoderManager transcoder)
            : base(stream, fallbackEncoding, log, logmanager, network, transcoder)
        {
            /* initialization per association can be done here */
        }

        public static void Start(int port, string aet)
        {
            Printer = new Printer(aet);
            _server = DicomServerFactory.Create<PrintService>(port);
        }

        public static void Stop()
        {
            _server.Dispose();
        }

        #endregion

        #region IDicomServiceProvider Members

        public Task OnReceiveAssociationRequestAsync(DicomAssociation association)
        {
            Logger.Info("Received association request from AE: {0} with IP: {1} ", association.CallingAE, association.RemoteHost);

            if (Printer.PrinterAet != association.CalledAE)
            {
                Logger.Error(
                    "Association with {0} rejected since requested printer {1} not found",
                    association.CallingAE,
                    association.CalledAE);
                return SendAssociationRejectAsync(
                    DicomRejectResult.Permanent,
                    DicomRejectSource.ServiceUser,
                    DicomRejectReason.CalledAENotRecognized);
            }

            CallingAE = association.CallingAE;
            CalledAE = Printer.PrinterAet;

            foreach (var pc in association.PresentationContexts)
            {
                if (pc.AbstractSyntax == DicomUID.Verification
                    || pc.AbstractSyntax == DicomUID.BasicGrayscalePrintManagementMeta
                    || pc.AbstractSyntax == DicomUID.BasicColorPrintManagementMeta
                    || pc.AbstractSyntax == DicomUID.Printer
                    || pc.AbstractSyntax == DicomUID.BasicFilmSession
                    || pc.AbstractSyntax == DicomUID.BasicFilmBox
                    || pc.AbstractSyntax == DicomUID.BasicGrayscaleImageBox
                    || pc.AbstractSyntax == DicomUID.BasicColorImageBox)
                {
                    pc.AcceptTransferSyntaxes(_acceptedTransferSyntaxes);
                }
                else if (pc.AbstractSyntax == DicomUID.PrintJob)
                {
                    pc.AcceptTransferSyntaxes(_acceptedTransferSyntaxes);
                    _sendEventReports = true;
                }
                else
                {
                    Logger.Warn(
                        "Requested abstract syntax {abstractSyntax} from {callingAE} not supported",
                        pc.AbstractSyntax,
                        association.CallingAE);
                    pc.SetResult(DicomPresentationContextResult.RejectAbstractSyntaxNotSupported);
                }
            }

            Logger.Info("Accepted association request from {callingAE}", association.CallingAE);
            return SendAssociationAcceptAsync(association);
        }

        public Task OnReceiveAssociationReleaseRequestAsync()
        {
            Clean();
            return SendAssociationReleaseResponseAsync();
        }

        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
            //stop printing operation
            //log the abort reason
            Logger.Error("Received abort from {0}, reason is {1}", source, reason);
        }

        public void OnConnectionClosed(Exception exception)
        {
            Clean();
        }

        #endregion


        #region IDicomCEchoProvider Members

        public Task<DicomCEchoResponse> OnCEchoRequestAsync(DicomCEchoRequest request)
        {
            Logger.Info("Received verification request from AE {0} with IP: {1}", CallingAE, Association.RemoteHost);
            return Task.FromResult(new DicomCEchoResponse(request, DicomStatus.Success));
        }

        #endregion

        #region N-CREATE requests handlers

        public Task<DicomNCreateResponse> OnNCreateRequestAsync(DicomNCreateRequest request)
        {
            lock (_synchRoot)
            {
                if (request.SOPClassUID == DicomUID.BasicFilmSession)
                {
                    return Task.FromResult(CreateFilmSession(request));
                }
                else if (request.SOPClassUID == DicomUID.BasicFilmBox)
                {
                    return Task.FromResult(CreateFilmBox(request));
                }
                else
                {
                    return Task.FromResult(new DicomNCreateResponse(request, DicomStatus.SOPClassNotSupported));
                }
            }
        }

        private DicomNCreateResponse CreateFilmSession(DicomNCreateRequest request)
        {
            if (_filmSession != null)
            {
                Logger.Error("Attemted to create new basic film session on association with {0}", CallingAE);
                SendAbortAsync(DicomAbortSource.ServiceProvider, DicomAbortReason.NotSpecified).Wait();
                return new DicomNCreateResponse(request, DicomStatus.NoSuchObjectInstance);
            }

            var pc = request.PresentationContext;

            bool isColor = pc != null && pc.AbstractSyntax == DicomUID.BasicColorPrintManagementMeta;

            _filmSession = new FilmSession(request.SOPClassUID, request.SOPInstanceUID, request.Dataset, isColor);

            Logger.Info("Create new film session {0}", _filmSession.SOPInstanceUID.UID);
            if (string.IsNullOrEmpty(request.SOPInstanceUID?.UID))
            {
                request.Command.AddOrUpdate(DicomTag.AffectedSOPInstanceUID, _filmSession.SOPInstanceUID);
            }
            var response = new DicomNCreateResponse(request, DicomStatus.Success);
            return response;
        }

        private DicomNCreateResponse CreateFilmBox(DicomNCreateRequest request)
        {
            if (_filmSession == null)
            {
                Logger.Error("A basic film session does not exist for this association {0}", CallingAE);
                SendAbortAsync(DicomAbortSource.ServiceProvider, DicomAbortReason.NotSpecified).Wait();
                return new DicomNCreateResponse(request, DicomStatus.NoSuchObjectInstance);
            }

            var filmBox = _filmSession.CreateFilmBox(request.SOPInstanceUID, request.Dataset);

            if (!filmBox.Initialize())
            {
                Logger.Error("Failed to initialize requested film box {0}", filmBox.SOPInstanceUID.UID);
                SendAbortAsync(DicomAbortSource.ServiceProvider, DicomAbortReason.NotSpecified).Wait();
                return new DicomNCreateResponse(request, DicomStatus.ProcessingFailure);
            }

            Logger.Info("Created new film box {0}", filmBox.SOPInstanceUID.UID);
            if (string.IsNullOrEmpty(request.SOPInstanceUID?.UID))
            {
                request.Command.AddOrUpdate(DicomTag.AffectedSOPInstanceUID, filmBox.SOPInstanceUID.UID);
            }

            return new DicomNCreateResponse(request, DicomStatus.Success)
            {
                Dataset = filmBox
            };
        }


        #endregion

        #region N-DELETE request handler

        public async Task<DicomNDeleteResponse> OnNDeleteRequestAsync(DicomNDeleteRequest request)
        {
            lock (_synchRoot)
            {
                if (request.SOPClassUID == DicomUID.BasicFilmSession)
                {
                    return DeleteFilmSession(request);
                }
                else if (request.SOPClassUID == DicomUID.BasicFilmBox)
                {
                    return DeleteFilmBox(request);
                }
                else
                {
                    return new DicomNDeleteResponse(request, DicomStatus.NoSuchSOPClass);
                }
            }
        }

        private DicomNDeleteResponse DeleteFilmBox(DicomNDeleteRequest request)
        {
            if (_filmSession == null)
            {
                Logger.Error("Can't delete a basic film session doesnot exist for this association {0}", CallingAE);
                return new DicomNDeleteResponse(request, DicomStatus.NoSuchObjectInstance);
            }

            DicomStatus status =
                _filmSession.DeleteFilmBox(request.SOPInstanceUID)
                ? DicomStatus.Success
                : DicomStatus.NoSuchObjectInstance;
            var response = new DicomNDeleteResponse(request, status);
            return response;
        }

        private DicomNDeleteResponse DeleteFilmSession(DicomNDeleteRequest request)
        {
            if (_filmSession == null)
            {
                Logger.Error("Can't delete a basic film session doesnot exist for this association {0}", CallingAE);
                return new DicomNDeleteResponse(request, DicomStatus.NoSuchObjectInstance);
            }

            if (!request.SOPInstanceUID.Equals(_filmSession.SOPInstanceUID))
            {
                Logger.Error(
                    "Can't delete a basic film session with instace UID {0} doesnot exist for this association {1}",
                    request.SOPInstanceUID.UID,
                    CallingAE);
                return new DicomNDeleteResponse(request, DicomStatus.NoSuchObjectInstance);
            }
            _filmSession = null;

            return new DicomNDeleteResponse(request, DicomStatus.Success);
        }

        #endregion

        #region N-SET request handler

        public Task<DicomNSetResponse> OnNSetRequestAsync(DicomNSetRequest request)
        {
            lock (_synchRoot)
            {
                if (request.SOPClassUID == DicomUID.BasicFilmSession)
                {
                    return Task.FromResult(SetFilmSession(request));
                }
                else if (request.SOPClassUID == DicomUID.BasicFilmBox)
                {
                    return Task.FromResult(SetFilmBox(request));
                }
                else if (request.SOPClassUID == DicomUID.BasicColorImageBox
                         || request.SOPClassUID == DicomUID.BasicGrayscaleImageBox)
                {
                    return Task.FromResult(SetImageBox(request));
                }
                else
                {
                    return Task.FromResult(new DicomNSetResponse(request, DicomStatus.SOPClassNotSupported));
                }
            }
        }

        private DicomNSetResponse SetImageBox(DicomNSetRequest request)
        {
            if (_filmSession == null)
            {
                Logger.Error("A basic film session does not exist for this association {0}", CallingAE);
                return new DicomNSetResponse(request, DicomStatus.NoSuchObjectInstance);
            }

            Logger.Info("Set image box {0}", request.SOPInstanceUID.UID);

            var imageBox = _filmSession.FindImageBox(request.SOPInstanceUID);
            if (imageBox == null)
            {
                Logger.Error(
                    "Received N-SET request for invalid image box instance {0} for this association {1}",
                    request.SOPInstanceUID.UID,
                    CallingAE);
                return new DicomNSetResponse(request, DicomStatus.NoSuchObjectInstance);
            }

            request.Dataset.CopyTo(imageBox);

            return new DicomNSetResponse(request, DicomStatus.Success);
        }

        private DicomNSetResponse SetFilmBox(DicomNSetRequest request)
        {
            if (_filmSession == null)
            {
                Logger.Error("A basic film session does not exist for this association {0}", CallingAE);
                return new DicomNSetResponse(request, DicomStatus.NoSuchObjectInstance);
            }

            Logger.Info("Set film box {0}", request.SOPInstanceUID.UID);
            var filmBox = _filmSession.FindFilmBox(request.SOPInstanceUID);

            if (filmBox == null)
            {
                Logger.Error(
                    "Received N-SET request for invalid film box {0} from {1}",
                    request.SOPInstanceUID.UID,
                    CallingAE);
                return new DicomNSetResponse(request, DicomStatus.NoSuchObjectInstance);
            }

            request.Dataset.CopyTo(filmBox);

            filmBox.Initialize();

            var response = new DicomNSetResponse(request, DicomStatus.Success);
            response.Command.AddOrUpdate(DicomTag.AffectedSOPInstanceUID, filmBox.SOPInstanceUID);
            response.Dataset = filmBox;
            return response;
        }

        private DicomNSetResponse SetFilmSession(DicomNSetRequest request)
        {
            if (_filmSession == null || _filmSession.SOPInstanceUID.UID != request.SOPInstanceUID.UID)
            {
                Logger.Error("A basic film session does not exist for this association {0}", CallingAE);
                return new DicomNSetResponse(request, DicomStatus.NoSuchObjectInstance);
            }

            Logger.Info("Set film session {0}", request.SOPInstanceUID.UID);
            request.Dataset.CopyTo(_filmSession);

            return new DicomNSetResponse(request, DicomStatus.Success);
        }

        #endregion

        #region N-GET request handler

        public async Task<DicomNGetResponse> OnNGetRequestAsync(DicomNGetRequest request)
        {
            lock (_synchRoot)
            {
                Logger.Info(request.ToString(true));

                if (request.SOPClassUID == DicomUID.Printer
                    && request.SOPInstanceUID == DicomUID.PrinterInstance)
                {
                    return GetPrinter(request);
                }
                else if (request.SOPClassUID == DicomUID.PrintJob)
                {
                    return GetPrintJob(request);
                }
                else if (request.SOPClassUID == DicomUID.PrinterConfigurationRetrieval
                         && request.SOPInstanceUID == DicomUID.PrinterConfigurationRetrievalInstance)
                {
                    return GetPrinterConfiguration(request);
                }
                else
                {
                    return new DicomNGetResponse(request, DicomStatus.NoSuchSOPClass);
                }
            }
        }

        private DicomNGetResponse GetPrinter(DicomNGetRequest request)
        {
            var ds = new DicomDataset();

            var sb = new StringBuilder();
            if (request.Attributes != null && request.Attributes.Length > 0)
            {
                foreach (var item in request.Attributes)
                {
                    sb.AppendFormat("GetPrinter attribute {0} requested", item);
                    sb.AppendLine();
                    var value = Printer.GetSingleValueOrDefault(item, "");
                    ds.Add(item, value);
                }

                Logger.Info(sb.ToString());
            }
            if (!ds.Any())
            {
                ds.Add(DicomTag.PrinterStatus, Printer.PrinterStatus);
                ds.Add(DicomTag.PrinterStatusInfo, "");
                ds.Add(DicomTag.PrinterName, Printer.PrinterName);
                ds.Add(DicomTag.Manufacturer, Printer.Manufacturer);
                ds.Add(DicomTag.DateOfLastCalibration, Printer.DateTimeOfLastCalibration.Date);
                ds.Add(DicomTag.TimeOfLastCalibration, Printer.DateTimeOfLastCalibration);
                ds.Add(DicomTag.ManufacturerModelName, Printer.ManufacturerModelName);
                ds.Add(DicomTag.DeviceSerialNumber, Printer.DeviceSerialNumber);
                ds.Add(DicomTag.SoftwareVersions, Printer.SoftwareVersions);
            }

            var response = new DicomNGetResponse(request, DicomStatus.Success)
            {
                Dataset = ds
            };

            Logger.Info(response.ToString(true));
            return response;
        }

        private DicomNGetResponse GetPrinterConfiguration(DicomNGetRequest request)
        {
            var dataset = new DicomDataset();
            var config = new DicomDataset();

            var sequence = new DicomSequence(DicomTag.PrinterConfigurationSequence, config);

            dataset.Add(sequence);

            var response = new DicomNGetResponse(request, DicomStatus.Success);
            response.Command.AddOrUpdate(DicomTag.AffectedSOPInstanceUID, request.SOPInstanceUID);
            response.Dataset = dataset;
            return response;
        }

        private DicomNGetResponse GetPrintJob(DicomNGetRequest request)
        {
            if (_printJobList.ContainsKey(request.SOPInstanceUID.UID))
            {
                var printJob = _printJobList[request.SOPInstanceUID.UID];

                var sb = new StringBuilder();

                var dataset = new DicomDataset();

                if (request.Attributes != null && request.Attributes.Length > 0)
                {
                    foreach (var item in request.Attributes)
                    {
                        sb.AppendFormat("GetPrintJob attribute {0} requested", item);
                        sb.AppendLine();
                        var value = printJob.GetSingleValueOrDefault(item, "");
                        dataset.Add(item, value);
                    }

                    Logger.Info(sb.ToString());
                }

                var response = new DicomNGetResponse(request, DicomStatus.Success)
                {
                    Dataset = dataset
                };
                return response;
            }
            else
            {
                var response = new DicomNGetResponse(request, DicomStatus.NoSuchObjectInstance);
                return response;
            }
        }

        #endregion

        #region N-ACTION request handler

        public Task<DicomNActionResponse> OnNActionRequestAsync(DicomNActionRequest request)
        {
            if (_filmSession == null)
            {
                Logger.Error("A basic film session does not exist for this association {0}", CallingAE);
                return Task.FromResult(new DicomNActionResponse(request, DicomStatus.InvalidObjectInstance));
            }

            lock (_synchRoot)
            {
                try
                {

                    var filmBoxList = new List<FilmBox>();
                    if (request.SOPClassUID == DicomUID.BasicFilmSession && request.ActionTypeID == 0x0001)
                    {
                        Logger.Info("Creating new print job for film session {0}", _filmSession.SOPInstanceUID.UID);
                        filmBoxList.AddRange(_filmSession.BasicFilmBoxes);
                    }
                    else if (request.SOPClassUID == DicomUID.BasicFilmBox && request.ActionTypeID == 0x0001)
                    {
                        Logger.Info("Creating new print job for film box {0}", request.SOPInstanceUID.UID);

                        var filmBox = _filmSession.FindFilmBox(request.SOPInstanceUID);
                        if (filmBox != null)
                        {
                            filmBoxList.Add(filmBox);
                        }
                        else
                        {
                            Logger.Error(
                                "Received N-ACTION request for invalid film box {0} from {1}",
                                request.SOPInstanceUID.UID,
                                CallingAE);
                            return Task.FromResult(new DicomNActionResponse(request, DicomStatus.NoSuchObjectInstance));
                        }
                    }
                    else
                    {
                        if (request.ActionTypeID != 0x0001)
                        {
                            Logger.Error(
                                "Received N-ACTION request for invalid action type {0} from {1}",
                                request.ActionTypeID,
                                CallingAE);
                            return Task.FromResult(new DicomNActionResponse(request, DicomStatus.NoSuchActionType));
                        }
                        else
                        {
                            Logger.Error(
                                "Received N-ACTION request for invalid SOP class {0} from {1}",
                                request.SOPClassUID,
                                CallingAE);
                            return Task.FromResult(new DicomNActionResponse(request, DicomStatus.NoSuchSOPClass));
                        }
                    }

                    var printJob = new PrintJob(null, Printer, CallingAE, Logger)
                    {
                        SendNEventReport = _sendEventReports
                    };
                    printJob.StatusUpdate += OnPrintJobStatusUpdate;

                    printJob.Print(filmBoxList);

                    if (printJob.Error == null)
                    {

                        var result = new DicomDataset
                  {
                     new DicomSequence(
                          DicomTag.ReferencedPrintJobSequenceRETIRED,
                          new DicomDataset(
                              new DicomUniqueIdentifier(DicomTag.ReferencedSOPClassUID, DicomUID.PrintJob)),
                          new DicomDataset(
                              new DicomUniqueIdentifier(
                                  DicomTag.ReferencedSOPInstanceUID,
                                  printJob.SOPInstanceUID)))
                  };

                        var response = new DicomNActionResponse(request, DicomStatus.Success);
                        response.Command.AddOrUpdate(DicomTag.AffectedSOPInstanceUID, request.SOPInstanceUID);
                        response.Dataset = result;

                        return Task.FromResult(response);
                    }
                    else
                    {
                        throw printJob.Error;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(
                        "Error occured during N-ACTION {0} for SOP class {1} and instance {2}",
                        request.ActionTypeID,
                        request.SOPClassUID.UID,
                        request.SOPInstanceUID.UID);
                    Logger.Error(ex.Message);
                    return Task.FromResult(new DicomNActionResponse(request, DicomStatus.ProcessingFailure));
                }
            }
        }

        private void OnPrintJobStatusUpdate(object sender, StatusUpdateEventArgs e)
        {
            var printJob = sender as PrintJob;
            if (printJob.SendNEventReport)
            {
                var reportRequest = new DicomNEventReportRequest(
                    printJob.SOPClassUID,
                    printJob.SOPInstanceUID,
                    e.EventTypeId);
                var ds = new DicomDataset
            {
               { DicomTag.ExecutionStatusInfo, e.ExecutionStatusInfo },
               { DicomTag.FilmSessionLabel, e.FilmSessionLabel },
               { DicomTag.PrinterName, e.PrinterName }
            };

                reportRequest.Dataset = ds;
                SendRequestAsync(reportRequest).Wait();
            }
        }

        #endregion

        public void Clean()
        {
            //delete the current active print job and film sessions
            lock (_synchRoot)
            {
                _filmSession = null;
                _printJobList.Clear();
            }
        }

        #region IDicomNServiceProvider Members

        public Task<DicomNEventReportResponse> OnNEventReportRequestAsync(DicomNEventReportRequest request)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
