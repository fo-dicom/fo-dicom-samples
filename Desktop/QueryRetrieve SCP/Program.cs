using Dicom.Log;
using System;

namespace QueryRetrieve_SCP
{
    class Program
    {
        static void Main(string[] args)
        {
            // Initialize log manager.
            LogManager.SetImplementation(ConsoleLogManager.Instance);

            int tmp;
            var port = args != null && args.Length > 0 && int.TryParse(args[0], out tmp) ? tmp : 8001;

            Console.WriteLine($"Starting QR SCP server with AET: QRSCP on port {port}");

            QRServer.Start(port, "QRSCP");

            Console.WriteLine("Press any key to stop the service");

            Console.Read();

            Console.WriteLine("Stopping QR service");

            QRServer.Stop();

        }
    }
}
