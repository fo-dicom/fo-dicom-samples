// Copyright (c) 2012-2023 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using Microsoft.Extensions.Logging;
using QueryRetrieve_SCP.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QueryRetrieve_SCP
{

    public class QRService : DicomService, IDicomServiceProvider, IDicomCFindProvider, IDicomCEchoProvider, IDicomCMoveProvider, IDicomCGetProvider
    {


        private static readonly DicomTransferSyntax[] _acceptedTransferSyntaxes = new DicomTransferSyntax[]
            {
                DicomTransferSyntax.ExplicitVRLittleEndian,
                DicomTransferSyntax.ExplicitVRBigEndian,
                DicomTransferSyntax.ImplicitVRLittleEndian
            };


        private static readonly DicomTransferSyntax[] _acceptedImageTransferSyntaxes = new DicomTransferSyntax[]
            {
                // Lossless
                DicomTransferSyntax.JPEGLSLossless,
                DicomTransferSyntax.JPEG2000Lossless,
                DicomTransferSyntax.JPEGProcess14SV1,
                DicomTransferSyntax.JPEGProcess14,
                DicomTransferSyntax.RLELossless,

                // Lossy
                DicomTransferSyntax.JPEGLSNearLossless,
                DicomTransferSyntax.JPEG2000Lossy,
                DicomTransferSyntax.JPEGProcess1,
                DicomTransferSyntax.JPEGProcess2_4,

                // Uncompressed
                DicomTransferSyntax.ExplicitVRLittleEndian,
                DicomTransferSyntax.ExplicitVRBigEndian,
                DicomTransferSyntax.ImplicitVRLittleEndian
            };


        public string CallingAE { get; protected set; }
        public string CalledAE { get; protected set; }


        public QRService(INetworkStream stream, Encoding fallbackEncoding, ILogger log, DicomServiceDependencies dependencies)
                : base(stream, fallbackEncoding, log, dependencies)
        {
            /* initialization per association can be done here */
        }


        public Task<DicomCEchoResponse> OnCEchoRequestAsync(DicomCEchoRequest request)
        {
            Logger.LogInformation($"Received verification request from AE {CallingAE} with IP: {Association.RemoteHost}");
            return Task.FromResult(new DicomCEchoResponse(request, DicomStatus.Success));
        }


        public void OnConnectionClosed(Exception exception)
        {
            Clean();
        }


        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
            //log the abort reason
            Logger.LogError($"Received abort from {source}, reason is {reason}");
        }


        public Task OnReceiveAssociationReleaseRequestAsync()
        {
            Clean();
            return SendAssociationReleaseResponseAsync();
        }


        public Task OnReceiveAssociationRequestAsync(DicomAssociation association)
        {
            CallingAE = association.CallingAE;
            CalledAE = association.CalledAE;

            Logger.LogInformation($"Received association request from AE: {CallingAE} with IP: {association.RemoteHost} ");

            if (QRServer.AETitle != CalledAE)
            {
                Logger.LogError($"Association with {CallingAE} rejected since called aet {CalledAE} is unknown");
                return SendAssociationRejectAsync(DicomRejectResult.Permanent, DicomRejectSource.ServiceUser, DicomRejectReason.CalledAENotRecognized);
            }

            foreach (var pc in association.PresentationContexts)
            {
                if (pc.AbstractSyntax == DicomUID.Verification
                    || pc.AbstractSyntax == DicomUID.PatientRootQueryRetrieveInformationModelFind
                    || pc.AbstractSyntax == DicomUID.PatientRootQueryRetrieveInformationModelMove
                    || pc.AbstractSyntax == DicomUID.StudyRootQueryRetrieveInformationModelFind
                    || pc.AbstractSyntax == DicomUID.StudyRootQueryRetrieveInformationModelMove)
                {
                    pc.AcceptTransferSyntaxes(_acceptedTransferSyntaxes);
                }
                else if (pc.AbstractSyntax == DicomUID.PatientRootQueryRetrieveInformationModelGet
                    || pc.AbstractSyntax == DicomUID.StudyRootQueryRetrieveInformationModelGet)
                {
                    pc.AcceptTransferSyntaxes(_acceptedImageTransferSyntaxes);
                }
                else if (pc.AbstractSyntax.StorageCategory != DicomStorageCategory.None)
                {
                    pc.AcceptTransferSyntaxes(_acceptedImageTransferSyntaxes);
                }
                else
                {
                    Logger.LogWarning($"Requested abstract syntax {pc.AbstractSyntax} from {CallingAE} not supported");
                    pc.SetResult(DicomPresentationContextResult.RejectAbstractSyntaxNotSupported);
                }
            }

            Logger.LogInformation($"Accepted association request from {CallingAE}");
            return SendAssociationAcceptAsync(association);
        }


        public async IAsyncEnumerable<DicomCFindResponse> OnCFindRequestAsync(DicomCFindRequest request)
        {
            var queryLevel = request.Level;

            var matchingFiles = new List<string>();
            IDicomImageFinderService finderService = QRServer.CreateFinderService;

            // a QR SCP has to define in a DICOM Conformance Statement for which dicom tags it can query
            // depending on the level of the query. Below there are only very few parameters evaluated.

            switch (queryLevel)
            {
                case DicomQueryRetrieveLevel.Patient:
                    {
                        var patname = request.Dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);
                        var patid = request.Dataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty);

                        matchingFiles = finderService.FindPatientFiles(patname, patid);
                    }
                    break;

                case DicomQueryRetrieveLevel.Study:
                    {
                        var patname = request.Dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);
                        var patid = request.Dataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty);
                        var accNr = request.Dataset.GetSingleValueOrDefault(DicomTag.AccessionNumber, string.Empty);
                        var studyUID = request.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty);

                        matchingFiles = finderService.FindStudyFiles(patname, patid, accNr, studyUID);
                    }
                    break;

                case DicomQueryRetrieveLevel.Series:
                    {
                        var patname = request.Dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);
                        var patid = request.Dataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty);
                        var accNr = request.Dataset.GetSingleValueOrDefault(DicomTag.AccessionNumber, string.Empty);
                        var studyUID = request.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty);
                        var seriesUID = request.Dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty);
                        var modality = request.Dataset.GetSingleValueOrDefault(DicomTag.Modality, string.Empty);

                        matchingFiles = finderService.FindSeriesFiles(patname, patid, accNr, studyUID, seriesUID, modality);
                    }
                    break;

                case DicomQueryRetrieveLevel.Image:
                    yield return new DicomCFindResponse(request, DicomStatus.QueryRetrieveUnableToProcess);
                    yield break;
            }

            // now read the required dicomtags from the matching files and return as results
            foreach (var matchingFile in matchingFiles)
            {
                var dicomFile = DicomFile.Open(matchingFile);
                var result = new DicomDataset();
                foreach (var requestedTag in request.Dataset)
                {
                    // most of the requested DICOM tags are stored in the DICOM files and therefore saved into a database.
                    // you can fill the responses by selecting the values from the database.
                    // also be aware that there are some requested DicomTags like "ModalitiesInStudy" or "NumberOfStudyRelatedInstances" 
                    // or "NumberOfPatientRelatedInstances" and so on which have to be calculated and cannot be read from a DICOM file.
                    if (dicomFile.Dataset.Contains(requestedTag.Tag))
                    {
                        dicomFile.Dataset.CopyTo(result, requestedTag.Tag);
                    }
                    // else if (requestedTag == DicomTag.NumberOfStudyRelatedInstances)
                    // {
                    //    ... somehow calculate how many instances are stored within the study
                    //    result.Add(DicomTag.NumberOfStudyRelatedInstances, number);
                    // } ....
                    else
                    {
                        result.Add(requestedTag);
                    }
                }
                yield return new DicomCFindResponse(request, DicomStatus.Pending) { Dataset = result };
            }

            yield return new DicomCFindResponse(request, DicomStatus.Success);
        }


        public void Clean()
        {
            // cleanup, like cancel outstanding move- or get-jobs
        }


        public async IAsyncEnumerable<DicomCMoveResponse> OnCMoveRequestAsync(DicomCMoveRequest request)
        {
            // the c-move request contains the DestinationAE. the data of this AE should be configured somewhere.
            if (request.DestinationAE != "STORESCP")
            {
                yield return new DicomCMoveResponse(request, DicomStatus.QueryRetrieveMoveDestinationUnknown);
                yield return new DicomCMoveResponse(request, DicomStatus.ProcessingFailure);
                yield break;
            }

            // this data should come from some data storage!
            var destinationPort = 11112;
            var destinationIP = "localhost";

            IDicomImageFinderService finderService = QRServer.CreateFinderService;
            IEnumerable<string> matchingFiles = Enumerable.Empty<string>();

            switch (request.Level)
            {
                case DicomQueryRetrieveLevel.Patient:
                    matchingFiles = finderService.FindFilesByUID(request.Dataset.GetString(DicomTag.PatientID), string.Empty, string.Empty);
                    break;

                case DicomQueryRetrieveLevel.Study:
                    matchingFiles = finderService.FindFilesByUID(string.Empty, request.Dataset.GetString(DicomTag.StudyInstanceUID), string.Empty);
                    break;

                case DicomQueryRetrieveLevel.Series:
                    matchingFiles = finderService.FindFilesByUID(string.Empty, string.Empty, request.Dataset.GetString(DicomTag.SeriesInstanceUID));
                    break;

                case DicomQueryRetrieveLevel.Image:
                    yield return new DicomCMoveResponse(request, DicomStatus.QueryRetrieveUnableToPerformSuboperations);
                    yield break;
            }

            var client = DicomClientFactory.Create(destinationIP, destinationPort, false, QRServer.AETitle, request.DestinationAE);
            client.NegotiateAsyncOps();
            int storeTotal = matchingFiles.Count();
            int storeDone = 0; // this variable stores the number of instances that have already been sent
            int storeFailure = 0; // this variable stores the number of faulues returned in a OnResponseReceived
            foreach (string file in matchingFiles)
            {
                var storeRequest = new DicomCStoreRequest(file);
                // !!! there is a Bug in fo-dicom 3.0.2 that the OnResponseReceived handlers are invoked not until the DicomClient has already
                //     sent all the instances. So the counters are not increased image by image sent but only once in a bulk after all storage
                //     has been finished. This bug will be fixed hopefully soon.
                storeRequest.OnResponseReceived += (req, resp) =>
                {
                    if (resp.Status == DicomStatus.Success)
                    {
                        Logger.LogInformation("Storage of image successfull");
                        storeDone++;
                    }
                    else
                    {
                        Logger.LogError("Storage of image failed");
                        storeFailure++;
                    }
                };
                await client.AddRequestAsync(storeRequest);
            }

            var sendTask = client.SendAsync();

            while (!sendTask.IsCompleted)
            {
                // while the send-task is runnin we inform the QR SCU every 2 seconds about the status and how many instances are remaining to send. 
                yield return new DicomCMoveResponse(request, DicomStatus.Pending) { Remaining = storeTotal - storeDone - storeFailure, Completed = storeDone };
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }

            Logger.LogInformation("..finished");
            yield return new DicomCMoveResponse(request, DicomStatus.Success);
        }


        public async IAsyncEnumerable<DicomCGetResponse> OnCGetRequestAsync(DicomCGetRequest request)
        {
            IDicomImageFinderService finderService = QRServer.CreateFinderService;
            IEnumerable<string> matchingFiles = Enumerable.Empty<string>();

            switch (request.Level)
            {
                case DicomQueryRetrieveLevel.Patient:
                    matchingFiles = finderService.FindFilesByUID(request.Dataset.GetString(DicomTag.PatientID), string.Empty, string.Empty);
                    break;

                case DicomQueryRetrieveLevel.Study:
                    matchingFiles = finderService.FindFilesByUID(string.Empty, request.Dataset.GetString(DicomTag.StudyInstanceUID), string.Empty);
                    break;

                case DicomQueryRetrieveLevel.Series:
                    matchingFiles = finderService.FindFilesByUID(string.Empty, string.Empty, request.Dataset.GetString(DicomTag.SeriesInstanceUID));
                    break;

                case DicomQueryRetrieveLevel.Image:
                    yield return new DicomCGetResponse(request, DicomStatus.QueryRetrieveUnableToPerformSuboperations);
                    yield break;
            }

            foreach (var matchingFile in matchingFiles)
            {
                var storeRequest = new DicomCStoreRequest(matchingFile);
                await SendRequestAsync(storeRequest);
            }

            yield return new DicomCGetResponse(request, DicomStatus.Success);
        }


    }
}
