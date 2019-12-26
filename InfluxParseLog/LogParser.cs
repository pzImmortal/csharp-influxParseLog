using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using InfluxDB.LineProtocol.Payload;

namespace ru.pflb.InfluxParseLog
{
    class LogParser
    {
        private List<LineProtocolPayload> payload;
        private int size;
        private int counter;
        private int index;
        Dictionary<string, DateTime> startTime;

        /// <summary>
        ///     Initializes a new instance of LogParser class for parsing string lines and generating payload for influxdb
        /// </summary>
        /// <param name="size">Maximum writing points in one batch. Recommend of 5,000 to 10,000.</param>
        public LogParser(int size)
        {
            this.payload = new List<LineProtocolPayload>();
            this.payload.Add(new LineProtocolPayload());
            this.startTime = new Dictionary<string, DateTime>();
            this.size = size;
            this.counter = 0;
            this.index = 0;
        }

        //Line example: 25-12-2019 07:55:45.464 [35m[scheduling-1][0;39m [34mINFO [0;39m nt.updateRules - Dynamic.UpdateRules.Start
        public void ParseLine(string line)
        {
            string dtime = @"^(?<DTime>\d{2}-\d{2}-\d{4}\s+\d{2}:\d{2}:\d{2}\.\d+)";
            DateTime dt;

            string[] s = Regex.Split(line, @"", RegexOptions.IgnoreCase);

            Match m = Regex.Match(s[0], dtime);

            dt = Convert.ToDateTime(m.Value);

            string message = Regex.Split(s[4], @" - ", RegexOptions.IgnoreCase)[1].ToLower().Trim();

            if (!message.Contains("launching"))
            {
                if (message.Contains(".start"))
                {
                    startTime[message.Replace(".start", "")] = dt;
                }
                else
                if (message.Contains(".finish"))
                {
                    string objName = message.Replace(".finish", "");

                    AddPoint(objName, dt, objName);
                    if (objName == "dynamic.planing")
                    {
                        AddPoint("dynamic.updatebookingresources", dt, objName + ".iteration");
                    }
                }

            }

        }


        public List<LineProtocolPayload> GetPayload()
        {
            return payload;
        }

        public static long ToUnixTimestamp(DateTime target)
        {
            var date = new DateTime(1970, 1, 1, 0, 0, 0, target.Kind);
            var unixTimestamp = System.Convert.ToInt64((target - date).TotalMilliseconds);

            return unixTimestamp;
        }

        private void AddPoint(string key, DateTime currDT, string tag)
        {
            DateTime start;
            //long finish = ToUnixTimestamp(currDT);
            try
            {
                startTime.TryGetValue(key, out start);
            }
            catch
            {
                return;
            }
            var dur = currDT - start;

            var point = new LineProtocolPoint(
                "log",
                new Dictionary<string, object>
                {
                            { "duration", dur.TotalMilliseconds },
                },
                new Dictionary<string, string>
                {
                            { "host", Environment.GetEnvironmentVariable("COMPUTERNAME") },
                            { "object", tag }
                },
            currDT.ToUniversalTime());

            payload[index].Add(point);
            counter++;

            if (counter >= size)
            {
                payload.Add(new LineProtocolPayload());
                index++;
            }
        }

    }
}
