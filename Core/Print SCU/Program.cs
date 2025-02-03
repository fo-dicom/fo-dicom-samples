﻿// Copyright (c) 2012-2025 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).


// Initialize log manager.
using FellowOakDicom.Imaging;
using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FellowOakDicom.Samples.PrintSCU;

new DicomSetupBuilder()
    .RegisterServices(s => s
        .AddFellowOakDicom()
        .AddLogging(config => config.AddConsole())
        .AddImageManager<ImageSharpImageManager>())
    .Build();

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
using (var bitmap = greyscaleImg.RenderImage() as ImageSharpImage)
{
    printJob.FilmSession.IsColor = false; //set to true to print in color
    printJob.StartFilmBox("STANDARD\\1,1", "PORTRAIT", "A4");
    printJob.AddImage(bitmap, 0);
    printJob.EndFilmBox();
}

//color
var colorImg = new DicomImage(@"Data\US-RGB-8-epicard.dcm");
using (var bitmap = greyscaleImg.RenderImage() as ImageSharpImage)
{
    printJob.FilmSession.IsColor = true; //set to true to print in color
    printJob.StartFilmBox("STANDARD\\1,1", "PORTRAIT", "A4");
    printJob.AddImage(bitmap, 0);
    printJob.EndFilmBox();
}

await printJob.Print();

stopwatch.Stop();
Console.WriteLine();
Console.WriteLine(stopwatch.Elapsed);
