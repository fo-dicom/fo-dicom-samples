using Dicom.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryRetrieve_SCP
{
    public class QRService
    {

        private static IDicomServer _server;

        public static string AETitle { get; set; }


        public static void Start(int port, string aet)
        {
            AETitle = aet;
            _server = DicomServer.Create<QRServer>(port);
        }


        public static void Stop()
        {
            _server.Dispose();
        }


    }
}
