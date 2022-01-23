// Copyright (c) 2012-2022 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using System;
using FellowOakDicom;
using FellowOakDicom.Log;
using FellowOakDicom.Samples.Printing;

namespace Print_SCP
{

    internal static class Program
    {

        private static void Main(string[] args)
        {
            // Initialize log manager.
            new DicomSetupBuilder()
                   .RegisterServices(s => s.AddFellowOakDicom().AddLogManager<ConsoleLogManager>())
                   .Build();

            //This is a simple DICOM Print SCP implementation with Print Job and Send Event Report Support
            //This sample depends on the Microsoft XPS Document Writer Printer to be installed on the system
            //You are free to use what ever printer you like by modifying the PrintJob DoPrint method hard coded
            //printer name

            //All print jobs will be created to the exe folder under a folder named PrintJobs

            var port = args != null && args.Length > 0 && int.TryParse(args[0], out int tmp) ? tmp : 8000;

            Console.WriteLine($"Starting print SCP server with AET: PRINTSCP on port {port}");

            PrintService.Start(port, "PRINTSCP");

            Console.WriteLine("Press any key to stop the service");

            Console.Read();

            Console.WriteLine("Stopping print service");

            PrintService.Stop();

        }
    }
}
