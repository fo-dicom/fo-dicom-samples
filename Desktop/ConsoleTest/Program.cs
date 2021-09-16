// Copyright (c) 2012-2021 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using System;
using System.IO;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Log;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using NLog.Config;
using NLog.Targets;

namespace ConsoleTest
{
    internal static class Program
    {

        private static async Task Main(string[] args)
        {
            try
            {

                // Initialize log manager.
                new DicomSetupBuilder().RegisterServices(
                   s => s.AddFellowOakDicom().AddLogManager<NLogManager>()
                   ).Build();

                DicomException.OnException += delegate (object sender, DicomExceptionEventArgs ea)
                    {
                        ConsoleColor old = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(ea.Exception);
                        Console.ForegroundColor = old;
                    };

                var config = new LoggingConfiguration();

                var target = new ColoredConsoleTarget
                {
                    Layout = @"${date:format=HH\:mm\:ss}  ${message}"
                };
                config.AddTarget("Console", target);
                config.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Debug, target));

                NLog.LogManager.Configuration = config;

                var client = DicomClientFactory.Create("127.0.0.1", 11112, false, "SCU", "STORESCP");
                client.NegotiateAsyncOps();
                for (int i = 0; i < 10; i++)
                {
                    await client.AddRequestAsync(new DicomCEchoRequest());
                }

                await client.AddRequestAsync(new DicomCStoreRequest(@"test1.dcm"));
                await client.AddRequestAsync(new DicomCStoreRequest(@"test2.dcm"));
                await client.SendAsync();

                foreach (DicomPresentationContext ctr in client.AdditionalPresentationContexts)
                {
                    Console.WriteLine("PresentationContext: " + ctr.AbstractSyntax + " Result: " + ctr.Result);
                }

                var samplesDir = Path.Combine(
                    Path.GetPathRoot(Environment.CurrentDirectory),
                    "Development",
                    "fo-dicom-samples");
                var testDir = Path.Combine(samplesDir, "Test");

                if (!Directory.Exists(testDir))
                {
                    Directory.CreateDirectory(testDir);
                }

            }
            catch (Exception e)
            {
                if (!(e is DicomException))
                {
                    Console.WriteLine(e.ToString());
                }
            }

            Console.ReadLine();
        }
    }
}
