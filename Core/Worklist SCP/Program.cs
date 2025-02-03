// Copyright (c) 2012-2025 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

// Initialize log manager.
using FellowOakDicom;
using FellowOakDicom.Samples.WorklistSCP;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

new DicomSetupBuilder()
       .RegisterServices(s => s.AddFellowOakDicom().AddLogging(config => config.AddConsole()))
       .Build();

var port = args != null && args.Length > 0 && int.TryParse(args[0], out int tmp) ? tmp : 8005;

Console.WriteLine($"Starting QR SCP server with AET: QRSCP on port {port}");

WorklistServer.Start(port, "QRSCP");

Console.WriteLine("Press any key to stop the service");

Console.Read();

Console.WriteLine("Stopping QR service");

WorklistServer.Stop();
