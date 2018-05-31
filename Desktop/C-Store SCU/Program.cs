// Copyright (c) 2012-2018 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Dicom.Network;

namespace Dicom.CStoreSCU
{
   class Program
   {
      private static string StoreServerHost = "www.dicomserver.co.uk";
      private static int StoreServerPort = 11112;
      private static string StoreServerAET = "STORESCP";
      private static string AET = "FODICOMSCU";

      static void Main(string[] args)
      {
         var client = new DicomClient();
         client.NegotiateAsyncOps();
         var storeMore = "";

         StoreServerHost = GetServerHost();
         StoreServerPort = GetServerPort();

         Console.WriteLine("***************************************************");
         Console.WriteLine("Server AE Title: " + StoreServerAET);
         Console.WriteLine("Server Host Address: " + StoreServerHost);
         Console.WriteLine("Server Port: " + StoreServerPort);
         Console.WriteLine("Client AE Title: " + AET);
         Console.WriteLine("***************************************************");

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

               request.OnResponseReceived += (req, response) =>
               {
                  Console.WriteLine("C-Store Response Received, Status: " + response.Status);
               };

               client.AddRequest(request);
               client.Send(StoreServerHost, StoreServerPort, false, AET, StoreServerAET);
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
         var input = "";

         do
         {
            Console.WriteLine("Your local IP is: " + localIP);
            Console.WriteLine("Enter \"1\" to use your local IP Address: " + localIP);
            Console.WriteLine("Enter \"2\" to use defult: " + StoreServerHost);
            Console.WriteLine("Enter \"3\" to enter custom");
            Console.Write(">>>");

            input = Console.ReadLine().Trim().ToLower();

            if (input.Length > 0)
            {
               if (input[0] == '1')
               {
                  hostAddress = localIP;
               }
               else if (input[0] == '2')
               {
                  hostAddress = StoreServerHost;
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

         Console.WriteLine("Enter Server port, or \"Enter\" for default \"" + StoreServerPort + "\":");
         Console.Write(">>>");

         var input = Console.ReadLine().Trim();

         if (string.IsNullOrEmpty(input))
         {
            return StoreServerPort;
         }
         else
         {
            return int.Parse(input);
         }
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
