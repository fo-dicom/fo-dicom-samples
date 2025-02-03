// Copyright (c) 2012-2025 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Samples.CStoreSCP;


// getting port from command line argument or 11112
var port = args != null && args.Length > 0 && int.TryParse(args[0], out int tmp) ? tmp : 11112;
Console.WriteLine($"Starting C-Store SCP server on port {port}");

// setup DICOM configuration or services
new DicomSetupBuilder()
    .RegisterServices(s => s.AddFellowOakDicom())
.Build();

// Starting DICOM server on port
using (var server = DicomServerFactory.Create<StoreScp>(port))
{
    // end process
    Console.WriteLine("Press <return> to end...");
    Console.ReadLine();
}
