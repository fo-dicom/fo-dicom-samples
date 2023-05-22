// Copyright (c) 2012-2023 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace QueryRetrieve_SCP
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            // Initialize log manager.
            new DicomSetupBuilder()
                .RegisterServices(s => s.AddFellowOakDicom().AddLogging(config => config.AddConsole()))
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
