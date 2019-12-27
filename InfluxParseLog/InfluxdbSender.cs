using InfluxDB.LineProtocol.Client;
using InfluxDB.LineProtocol.Payload;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ru.pflb.InfluxParseLog
{
    class InfluxdbSender
    {
        private readonly string database;
        private readonly string serverBaseAddress;

        public InfluxdbSender(string influxdbUrl, string database)
        {
            this.database = database;
            serverBaseAddress = influxdbUrl;
        }

        public Task<LineProtocolWriteResult> WriteAsync(LineProtocolPayload source, CancellationToken cancellationToken = default)
        {
            if (source == null)
            {
                return Task.FromResult(new LineProtocolWriteResult(true, string.Empty));
            }

            var client = new LineProtocolClient(new Uri(serverBaseAddress), database);

            return client.WriteAsync(source, cancellationToken);
        }
    }
}
