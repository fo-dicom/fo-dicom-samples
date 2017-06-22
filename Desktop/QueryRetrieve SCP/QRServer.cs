using Dicom.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dicom.Log;
using Dicom;

namespace QueryRetrieve_SCP
{
    public class QRServer : DicomService, IDicomServiceProvider, IDicomCFindProvider, IDicomCEchoProvider
    {


        public static readonly DicomTransferSyntax[] AcceptedTransferSyntaxes = new DicomTransferSyntax[]
                                                                            {
                                                                                        DicomTransferSyntax
                                                                                            .ExplicitVRLittleEndian,
                                                                                        DicomTransferSyntax
                                                                                            .ExplicitVRBigEndian,
                                                                                        DicomTransferSyntax
                                                                                            .ImplicitVRLittleEndian
                                                                            };

        public static readonly DicomTransferSyntax[] AcceptedImageTransferSyntaxes = new DicomTransferSyntax[]
                                                                                        {
                                                                                         // Lossless
                                                                                         DicomTransferSyntax
                                                                                             .JPEGLSLossless,
                                                                                         DicomTransferSyntax
                                                                                             .JPEG2000Lossless,
                                                                                         DicomTransferSyntax
                                                                                             .JPEGProcess14SV1,
                                                                                         DicomTransferSyntax
                                                                                             .JPEGProcess14,
                                                                                         DicomTransferSyntax
                                                                                             .RLELossless,

                                                                                         // Lossy
                                                                                         DicomTransferSyntax
                                                                                             .JPEGLSNearLossless,
                                                                                         DicomTransferSyntax
                                                                                             .JPEG2000Lossy,
                                                                                         DicomTransferSyntax
                                                                                             .JPEGProcess1,
                                                                                         DicomTransferSyntax
                                                                                             .JPEGProcess2_4,

                                                                                         // Uncompressed
                                                                                         DicomTransferSyntax
                                                                                             .ExplicitVRLittleEndian,
                                                                                         DicomTransferSyntax
                                                                                             .ExplicitVRBigEndian,
                                                                                         DicomTransferSyntax
                                                                                             .ImplicitVRLittleEndian
                                                                                     };



        public string CallingAE { get; protected set; }
        public string CalledAE { get; protected set; }
        public System.Net.IPAddress RemoteIP { get; private set; }

        private readonly object _synchRoot = new object();


        public QRServer(INetworkStream stream, Encoding fallbackEncoding, Logger log) : base(stream, fallbackEncoding, log)
        {
            var pi = stream.GetType()
               .GetProperty(
                   "Socket",
                   System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (pi != null)
            {
                var endPoint =
                    ((System.Net.Sockets.Socket)pi.GetValue(stream, null)).RemoteEndPoint as System.Net.IPEndPoint;
                RemoteIP = endPoint.Address;
            }
            else
            {
                RemoteIP = new System.Net.IPAddress(new byte[] { 127, 0, 0, 1 });
            }
        }


        public DicomCEchoResponse OnCEchoRequest(DicomCEchoRequest request)
        {
            Logger.Info("Received verification request from AE {0} with IP: {1}", CallingAE, RemoteIP);
            return new DicomCEchoResponse(request, DicomStatus.Success);
        }


        public void OnConnectionClosed(Exception exception)
        {
            Clean();
        }

        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
            //log the abort reason
            Logger.Error("Received abort from {0}, reason is {1}", source, reason);
        }

        public void OnReceiveAssociationReleaseRequest()
        {
            Clean();
            SendAssociationReleaseResponse();
        }

        public void OnReceiveAssociationRequest(DicomAssociation association)
        {
            Logger.Info("Received association request from AE: {0} with IP: {1} ", association.CallingAE, RemoteIP);

            if (QRService.AETitle != association.CalledAE)
            {
                Logger.Error(
                    "Association with {0} rejected since called aet {1} is unknown",
                    association.CallingAE,
                    association.CalledAE);
                SendAssociationReject(
                    DicomRejectResult.Permanent,
                    DicomRejectSource.ServiceUser,
                    DicomRejectReason.CalledAENotRecognized);
                return;
            }

            CallingAE = association.CallingAE;
            CalledAE = association.CalledAE;

            foreach (var pc in association.PresentationContexts)
            {
                if (pc.AbstractSyntax == DicomUID.Verification
                    || pc.AbstractSyntax == DicomUID.PatientRootQueryRetrieveInformationModelFIND
                    || pc.AbstractSyntax == DicomUID.PatientRootQueryRetrieveInformationModelMOVE
                    || pc.AbstractSyntax == DicomUID.StudyRootQueryRetrieveInformationModelFIND
                    || pc.AbstractSyntax == DicomUID.StudyRootQueryRetrieveInformationModelMOVE)
                {
                    pc.AcceptTransferSyntaxes(AcceptedTransferSyntaxes);
                }
                else if (pc.AbstractSyntax == DicomUID.PatientRootQueryRetrieveInformationModelGET
                    || pc.AbstractSyntax == DicomUID.StudyRootQueryRetrieveInformationModelGET)
                {
                    pc.AcceptTransferSyntaxes(AcceptedImageTransferSyntaxes);
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
            SendAssociationAccept(association);
        }


        public IEnumerable<DicomCFindResponse> OnCFindRequest(DicomCFindRequest request)
        {
            throw new NotImplementedException();
        }

        public void Clean()
        {
            // cleanup, like cancel outstanding move- or get-jobs
        }


    }
}
