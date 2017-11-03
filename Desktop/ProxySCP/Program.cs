using Dicom;
using Dicom.Network;
using System;

namespace ProxySCP
{
    class Program
    {
        static void Main(string[] args)
        {
            // preload dictionary to prevent timeouts
            var dict = DicomDictionary.Default;


            // start DICOM server
            var serverMWL = DicomServer.Create<MWLProxySCP>(9107);      // host on 9107, forward request to 107
            var serverMPPS = DicomServer.Create<MPPSProxySCP>(9108);    // host on 9108, forward request to 108
            Console.WriteLine("MWL & MPPS Proxy started.");

            // end process
            Console.WriteLine("Press <return> to end...");
            Console.ReadLine();
        }
    }
}
