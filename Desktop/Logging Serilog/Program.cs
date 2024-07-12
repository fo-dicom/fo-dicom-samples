// Copyright (c) 2012-2024 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using System;
using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Enrichers;
using ILogger = Serilog.ILogger;

namespace Dicom.Demo.SerilogDemo
{
    internal class Program
    {

        //Set this to false if Seq (http://getseq.net) is not present
        private static bool useSeq = true;

        private static void Main(string[] args)
        {
            var serilogLogger = UseGlobalSerilogLogger();

            new DicomSetupBuilder()
                .RegisterServices(services => services.AddLogging(logging => logging.AddSerilog(serilogLogger)))
                .Build();
                

            //Do some DICOM work
            var file = DicomFile.Open(@"..\..\..\DICOM Media\Data\Patient1\2.dcm");

            //Example of logging a dicom dataset
            //file.Dataset.WriteToLog(LogManager.Default.GetLogger("dumpedDataset"), LogLevel.Info);

            //Other logging using fo-dicom's log abstraction
            serilogLogger.Fatal("A fatal message at {dateTime}", DateTime.Now);
            serilogLogger.Debug("A debug for file {filename} - info: {@metaInfo}", file.File.Name, file.FileMetaInfo);

            Console.WriteLine("Finished - hit enter to exit");
            Console.ReadLine();


        }


        private static ILogger UseSpecificSerilogLogger()
        {
            //Get a Serilog logger instance
            var logger = ConfigureLogging();

            //Wrap it in some extra context as an example
            logger = logger.ForContext("Purpose", "Demonstration");

            //Configure fo-dicom & Serilog
            return logger;
        }

        private static ILogger UseGlobalSerilogLogger()
        {
            //Configure logging
            var logger = ConfigureLogging();

            //Configure fo-dicom & Serilog
            return logger;
        }


        /// <summary>
        /// Create and return a serilog ILogger instance.  
        /// For convenience this also sets the global Serilog.Log instance
        /// </summary>
        /// <returns></returns>
        public static ILogger ConfigureLogging()
        {
            var loggerConfig = new LoggerConfiguration()
                //Enrich each log message with the machine name
                .Enrich.With<MachineNameEnricher>()
                //Accept verbose output  (there is effectively no filter)
                .MinimumLevel.Verbose()
                //Write out to the console using the "Literate" console sink (colours the text based on the logged type)
                .WriteTo.Console()
                //Also write out to a file based on the date and restrict these writes to warnings or worse (warning, error, fatal)
                .WriteTo.File(@"Warnings_{Date}.txt", global::Serilog.Events.LogEventLevel.Warning);

            var logger = loggerConfig
                //Take all of that configuration and make a logger
                .CreateLogger();

            //Stash the logger in the global Log instance for convenience
            global::Serilog.Log.Logger = logger;

            return logger;
        }
    }
}
