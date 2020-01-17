// Copyright (c) 2012-2020 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using System;
using System.Drawing;
using Dicom.Imaging;
using Dicom.Log;

namespace Print_SCU
{

    internal static class Program
    {
        private static async System.Threading.Tasks.Task Main(string[] args)
        {
            // Initialize log manager.
            LogManager.SetImplementation(ConsoleLogManager.Instance);

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            var printJob = new PrintJob("DICOM PRINT JOB")
            {
               RemoteAddress = "localhost",
               RemotePort = 8000,
               CallingAE = "PRINTSCU",
               CalledAE = "PRINTSCP"
            };

            //greyscale
            var greyscaleImg = new DicomImage(@"Data\1.3.51.5155.1353.20020423.1100947.1.0.0.dcm");
            using (var bitmap = greyscaleImg.RenderImage().As<Bitmap>())
            {
                printJob.StartFilmBox("STANDARD\\1,1", "PORTRAIT", "A4");
                printJob.FilmSession.IsColor = false; //set to true to print in color
                printJob.AddImage(bitmap, 0);
                printJob.EndFilmBox();
            }

            //color
            var colorImg = new DicomImage(@"Data\US-RGB-8-epicard.dcm");
            using (var bitmap = colorImg.RenderImage().As<Bitmap>())
            {
                printJob.StartFilmBox("STANDARD\\1,1", "PORTRAIT", "A4");
                printJob.FilmSession.IsColor = true; //set to true to print in color
                printJob.AddImage(bitmap, 0);
                printJob.EndFilmBox();
            }

            await printJob.Print();

            stopwatch.Stop();
            Console.WriteLine();
            Console.WriteLine(stopwatch.Elapsed);
        }
    }
}
