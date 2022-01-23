// Copyright (c) 2012-2022 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using FellowOakDicom.Network;
using QueryRetrieve_SCP.Model;

namespace QueryRetrieve_SCP
{

    public static class QRServer
    {

        private static IDicomServer _server;

        public static string AETitle { get; set; }


        public static IDicomImageFinderService CreateFinderService => new StupidSlowFinderService();


        public static void Start(int port, string aet)
        {
            AETitle = aet;
            _server = DicomServerFactory.Create<QRService>(port);
        }


        public static void Stop()
        {
            _server.Dispose();
        }


    }
}
