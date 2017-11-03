using Dicom;
using Dicom.Log;
using Dicom.Network;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ProxySCP
{
    public class MWLProxySCP : CoreProxySCP, IDicomCFindProvider
    {
        public MWLProxySCP(INetworkStream stream, Encoding fallbackEncoding, Logger log)
                : base(stream, fallbackEncoding, log)
            {
        }

        protected override IList<DicomUID> GetSupportedFeatures()
        {
            return new List<DicomUID>() {
                DicomUID.ModalityWorklistInformationModelFIND
            };
        }

        IEnumerable<DicomCFindResponse> IDicomCFindProvider.OnCFindRequest(DicomCFindRequest request)
        {
            var signal = new ManualResetEventSlim(false);

            var worklist = new List<DicomCFindResponse>();

            var client = new DicomClient();
            client.NegotiateAsyncOps();

            var newRequest = CloneRequest(request);
            newRequest.OnResponseReceived += (req, response) =>
            {
                var result = new DicomCFindResponse(request, response.Status)
                {
                    Dataset = response.Dataset
                };
                worklist.Add(result);

                if (response.Status != DicomStatus.Pending)
                {
                    signal.Set();
                }
            };
            client.AddRequest(newRequest);
            client.Send("localhost", 107, false, "scu", "scp");

            signal.Wait();
            return worklist;
        }

        private DicomCFindRequest CloneRequest(DicomCFindRequest req)
        {
            var request = new DicomCFindRequest(req.Command)
            {
                Dataset = req.Dataset
            };

            return request;
        }
    }
}
