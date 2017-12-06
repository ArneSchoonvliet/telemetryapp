using Microsoft.AspNetCore.SignalR.Client;
using rF2SMMonitor.rFactor2Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TelemetryApp
{
    public class ConnectionManager : IConnectionManager
    {
        private IMappedDoubleBuffer<rF2Scoring> _scoringBuffer;
        private IMappedDoubleBuffer<rF2Telemetry> _telemetryBuffer;
        public ConnectionManager(IMappedDoubleBuffer<rF2Telemetry> telemetryBuffer, IMappedDoubleBuffer<rF2Scoring> scoringBuffer)
        {
            _scoringBuffer = scoringBuffer ?? throw new ArgumentNullException(nameof(scoringBuffer));
            _telemetryBuffer = telemetryBuffer ?? throw new ArgumentNullException(nameof(telemetryBuffer));
        }

        public async Task<HubConnection> Connect()
        {
            while (true)
            {
                Thread.Sleep(1000);
                try
                {
                    _scoringBuffer.Connect();
                    _telemetryBuffer.Connect();
                    break;
                }
                catch (Exception)
                {
                    _scoringBuffer.Disconnect();
                    _telemetryBuffer.Disconnect();
                }
            }

            var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:64784/chat")
                .WithConsoleLogger()
                .Build();

            connection.On<Telemetry>("Send", x =>
            {
                Console.WriteLine("");
            });
            await connection.StartAsync();

            return connection;
        }

        public async Task Disconnect(HubConnection connection)
        {
            _scoringBuffer.Disconnect();
            _telemetryBuffer.Disconnect();

            await connection.DisposeAsync();
        }
    }

    public interface IConnectionManager
    {
        Task<HubConnection> Connect();
        Task Disconnect(HubConnection connection);
    }
}
