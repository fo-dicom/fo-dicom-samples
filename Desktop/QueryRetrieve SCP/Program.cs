// Copyright (c) 2012-2022 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using FellowOakDicom;
using FellowOakDicom.Log;
using System;

namespace QueryRetrieve_SCP
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            // Initialize log manager.
            new DicomSetupBuilder()
                .RegisterServices(s => s.AddFellowOakDicom().AddLogManager<ConsoleLogManager>())
                .Build();

            var port = args != null && args.Length > 0 && int.TryParse(args[0], out int tmp) ? tmp : 8001;

            Console.WriteLine($"Starting QR SCP server with AET: QRSCP on port {port}");

            QRServer.Start(port, "QRSCP");

            Console.WriteLine("Press any key to stop the service");

            Console.Read();

            Console.WriteLine("Stopping QR service");

            QRServer.Stop();

        }
    }
}
