using Dicom;
using Dicom.Log;
using Dicom.Network;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ProxySCP
{
    public class MPPSProxySCP : CoreProxySCP, IDicomNServiceProvider
    {
        public MPPSProxySCP(INetworkStream stream, Encoding fallbackEncoding, Logger log) : base(stream, fallbackEncoding, log)
        {
        }
        protected override IList<DicomUID> GetSupportedFeatures()
        {
            return new List<DicomUID>() {
                DicomUID.ModalityPerformedProcedureStepSOPClass
            };
        }

        public DicomNActionResponse OnNActionRequest(DicomNActionRequest request)
        {

            throw new System.NotImplementedException();
        }

        public DicomNCreateResponse OnNCreateRequest(DicomNCreateRequest request)
        {
            var signal = new ManualResetEventSlim(false);
            DicomNCreateResponse result = null;

            var client = new DicomClient();
            client.NegotiateAsyncOps();

            var newRequest = CloneNCreateRequest(request);
            newRequest.OnResponseReceived += (req, response) =>
            {
                result = new DicomNCreateResponse(request, response.Status)
                {
                    Dataset = response.Dataset
                };

                signal.Set();
            };
            client.AddRequest(newRequest);
            client.Send("localhost", 108, false, "scu", "scp");

            signal.Wait();
            return result;
        }
        public DicomNSetResponse OnNSetRequest(DicomNSetRequest request)
        {
            var signal = new ManualResetEventSlim(false);
            DicomNSetResponse result = null;

            var client = new DicomClient();
            client.NegotiateAsyncOps();

            var newRequest = CloneOnNSetRequest(request);
            newRequest.OnResponseReceived += (req, response) =>
            {
                result = new DicomNSetResponse(request, response.Status);
                result.Dataset = response.Dataset;

                signal.Set();
            };
            client.AddRequest(newRequest);
            client.Send("localhost", 108, false, "scu", "scp");

            signal.Wait();
            return result;
        }

        public DicomNDeleteResponse OnNDeleteRequest(DicomNDeleteRequest request)
        {
            throw new System.NotImplementedException();
        }

        public DicomNEventReportResponse OnNEventReportRequest(DicomNEventReportRequest request)
        {
            throw new System.NotImplementedException();
        }

        public DicomNGetResponse OnNGetRequest(DicomNGetRequest request)
        {
            throw new System.NotImplementedException();
        }

        private DicomNCreateRequest CloneNCreateRequest(DicomNCreateRequest req)
        {
            var request = new DicomNCreateRequest(req.Command);
            request.Dataset = req.Dataset;

            return request;
        }

        private DicomNSetRequest CloneOnNSetRequest(DicomNSetRequest req)
        {
            var request = new DicomNSetRequest(req.Command)
            {
                Dataset = req.Dataset
            };

            return request;
        }
    }
}
