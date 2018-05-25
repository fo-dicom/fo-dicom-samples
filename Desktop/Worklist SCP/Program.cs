// Copyright (c) 2012-2018 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using Dicom.Log;
using System;

namespace Worklist_SCP
{

    class Program
    {

        static void Main(string[] args)
        {
            // Initialize log manager.
            LogManager.SetImplementation(ConsoleLogManager.Instance);

            var port = args != null && args.Length > 0 && int.TryParse(args[0], out int tmp) ? tmp : 8005;

            Console.WriteLine($"Starting QR SCP server with AET: QRSCP on port {port}");

            WorklistServer.Start(port, "QRSCP");

            Console.WriteLine("Press any key to stop the service");

            Console.Read();

            Console.WriteLine("Stopping QR service");

            WorklistServer.Stop();
        }

    }
}
