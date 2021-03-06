﻿using NLog;
using NLog.Config;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Configuration;

namespace ru.pflb.InfluxParseLog
{
    class Program
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        // The ConnectionString used to decide which database to connect to:
        private static string serverBaseAddress = "http://localhost:8086";

        private static string database = "TestC";

        static void Main(string[] args)
        {
            Stopwatch watch = System.Diagnostics.Stopwatch.StartNew(); //DEBUG

            LogManager.Configuration = new XmlLoggingConfiguration("nlog.config");

            serverBaseAddress = ConfigurationManager.AppSettings.Get("influxServerBaseAddress");
            database = ConfigurationManager.AppSettings.Get("influxDatabase");

            ProcessLog();

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            log.Info("Importing logs for influxdb has finished. Excecution time: " + elapsedMs + " ms");
        }

        private static void ProcessLog()
        {
            var logDataFiles = GetLogsFromFolder(Directory.GetCurrentDirectory());

            foreach (var logDataFile in logDataFiles)
            {
                ProcessLog(logDataFile);
            }
        }


        private static void ProcessLog(string logFilePath)
        {
            log.Info($"Processing File: {logFilePath}");

            var influx = new InfluxdbSender(serverBaseAddress, database);
            var parser = new LogParser(10000);

            string line;

            // Read the file line by line.  
            System.IO.StreamReader file = new System.IO.StreamReader(logFilePath);
            while ((line = file.ReadLine()) != null)
            {
                //Parse line and save measurements in payload
                parser.ParseLine(line);
            }

            file.Close();

            //Return payload from parser
            var payload = parser.GetPayload();

            //Send each batch of payload in influxdb
            foreach (var batch in payload)
            {
                try
                {
                    var result = influx.WriteAsync(batch).GetAwaiter().GetResult();
                    // Log all unsuccessful writes, but do not quit execution:
                    if (!result.Success)
                    {
                        log.Error(result.ErrorMessage);
                    }
                }
                catch (Exception e)
                {
                    // Some Pokemon Exception Handling here. I am seeing TaskCanceledExceptions with the 
                    // InfluxDB .NET Client. At the same time I do not want to quit execution, because 
                    // some batches fail:
                    log.Error(e, "Error occured writing InfluxDB Payload");
                }
            }
        }

        private static string[] GetLogsFromFolder(string directory)
        {
            return Directory.GetFiles(directory, "*.log").ToArray();
        }


    }
}
