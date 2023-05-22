// Copyright (c) 2012-2023 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;

namespace FellowOakDicom.Samples.CStoreSCU
{
    internal static class Program
    {

        private static string _storeServerHost = "www.dicomserver.co.uk";
        private static int _storeServerPort = 11112;
        private const string _storeServerAET = "STORESCP";
        private const string _aet = "FODICOMSCU";

        static async Task Main(string[] args)
        {
            var storeMore = "";

            _storeServerHost = GetServerHost();
            _storeServerPort = GetServerPort();

            Console.WriteLine("***************************************************");
            Console.WriteLine("Server AE Title: " + _storeServerAET);
            Console.WriteLine("Server Host Address: " + _storeServerHost);
            Console.WriteLine("Server Port: " + _storeServerPort);
            Console.WriteLine("Client AE Title: " + _aet);
            Console.WriteLine("***************************************************");

            var client = DicomClientFactory.Create(_storeServerHost, _storeServerPort, false, _aet, _storeServerAET);
            client.NegotiateAsyncOps();

            do
            {
                try
                {
                    Console.WriteLine();
                    Console.WriteLine("Enter the path for a DICOM file:");
                    Console.Write(">>>");
                    string dicomFile = Console.ReadLine();

                    while (!File.Exists(dicomFile))
                    {
                        Console.WriteLine("Invalid file path, enter the path for a DICOM file or press Enter to Exit:");

                        dicomFile = Console.ReadLine();

                        if (string.IsNullOrWhiteSpace(dicomFile))
                        {
                            return;
                        }
                    }

                    var request = new DicomCStoreRequest(dicomFile);

                    request.OnResponseReceived += (req, response) => Console.WriteLine("C-Store Response Received, Status: " + response.Status);

                    await client.AddRequestAsync(request);
                    await client.SendAsync();
                }
                catch (Exception exception)
                {
                    Console.WriteLine();
                    Console.WriteLine("----------------------------------------------------");
                    Console.WriteLine("Error storing file. Exception Details:");
                    Console.WriteLine(exception.ToString());
                    Console.WriteLine("----------------------------------------------------");
                    Console.WriteLine();
                }

                Console.WriteLine("To store another file, enter \"y\"; Othersie, press enter to exit: ");
                Console.Write(">>>");
                storeMore = Console.ReadLine().Trim();

            } while (storeMore.Length > 0 && storeMore.ToLower()[0] == 'y');
        }

        private static string GetServerHost()
        {
            var hostAddress = "";
            var localIP = GetLocalIPAddress();
            do
            {
                Console.WriteLine("Your local IP is: " + localIP);
                Console.WriteLine("Enter \"1\" to use your local IP Address: " + localIP);
                Console.WriteLine("Enter \"2\" to use defult: " + _storeServerHost);
                Console.WriteLine("Enter \"3\" to enter custom");
                Console.Write(">>>");

                string input = Console.ReadLine().Trim().ToLower();

                if (input.Length > 0)
                {
                    if (input[0] == '1')
                    {
                        hostAddress = localIP;
                    }
                    else if (input[0] == '2')
                    {
                        hostAddress = _storeServerHost;
                    }
                    else if (input[0] == '3')
                    {
                        Console.WriteLine("Enter Server Host Address:");
                        Console.Write(">>>");

                        hostAddress = Console.ReadLine();
                    }
                }
            } while (hostAddress.Length == 0);


            return hostAddress;
        }

        private static int GetServerPort()
        {

            Console.WriteLine("Enter Server port, or \"Enter\" for default \"" + _storeServerPort + "\":");
            Console.Write(">>>");

            var input = Console.ReadLine().Trim();

            return string.IsNullOrEmpty(input) ? _storeServerPort : int.Parse(input);
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }

            return "";
        }
    }
}
