using rF2SMMonitor;
using rF2SMMonitor.rFactor2Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using TelemetryApp;

namespace TelemetryApp
{
    public class ApplicationManager
    {
        private IMappedDoubleBuffer<rF2Scoring> _scoringBuffer;
        private readonly IConnectionManager _connectionManager;
        private IMappedDoubleBuffer<rF2Telemetry> _telemetryBuffer;
        public ApplicationManager(IMappedDoubleBuffer<rF2Telemetry> telemetryBuffer, IMappedDoubleBuffer<rF2Scoring> scoringBuffer, IConnectionManager connectionManager)
        {
            _scoringBuffer = scoringBuffer ?? throw new ArgumentNullException(nameof(scoringBuffer));
            _telemetryBuffer = telemetryBuffer ?? throw new ArgumentNullException(nameof(telemetryBuffer));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        public async Task Run(CancellationToken token)
        {
            var hubConnection = await _connectionManager.Connect();

            var telemetry = new rF2Telemetry();
            var scoring = new rF2Scoring();

            var playerScoring = new rF2VehicleScoring();
            var playerTelemetry = new rF2VehicleTelemetry();

            while (true)
            {
                await Task.Delay(200);
                try
                {
                    _scoringBuffer.GetMappedDataPartial(ref scoring);
                    _telemetryBuffer.GetMappedDataPartial(ref telemetry);
                }
                catch (Exception)
                {
                    await _connectionManager.Disconnect(hubConnection);
                    return;
                }

                if (telemetry.mNumVehicles != 0 && scoring.mScoringInfo.mNumVehicles != 0 && (rFactor2Constants.rF2GamePhase)scoring.mScoringInfo.mGamePhase == rFactor2Constants.rF2GamePhase.GreenFlag)
                {
                    var mappedIds = MapmIdToPositionInArray(ref telemetry);

                    foreach (var scoringVehicle in scoring.mVehicles)
                    {
                        switch ((rFactor2Constants.rF2Control)scoringVehicle.mControl)
                        {
                            case rFactor2Constants.rF2Control.AI:
                            case rFactor2Constants.rF2Control.Player:
                            case rFactor2Constants.rF2Control.Remote:
                                if (scoringVehicle.mIsPlayer == 1)
                                    playerScoring = scoringVehicle;
                                break;
                            default:
                                continue;
                        }
                        if (playerScoring.mIsPlayer == 1) break;
                    }

                    var playerTelemetryId = -1;
                    if (mappedIds.TryGetValue(playerScoring.mID, out playerTelemetryId))
                        playerTelemetry = telemetry.mVehicles[playerTelemetryId];
                    else continue;

                    var telemetryToSend = new Test
                    {
                        Telemetry = new Telemetry(ref playerTelemetry)
                    };
                    await hubConnection.SendAsync("Send", telemetryToSend);
                }
            }
        }

        #region Private methods
        private Dictionary<long, int> MapmIdToPositionInArray(ref rF2Telemetry telemetry)
        {
            var idsToTelIndices = new Dictionary<long, int>();
            for (int i = 0; i < telemetry.mNumVehicles; ++i)
            {
                if (!idsToTelIndices.ContainsKey(telemetry.mVehicles[i].mID))
                    idsToTelIndices.Add(telemetry.mVehicles[i].mID, i);
            }

            return idsToTelIndices;
        }
        #endregion
    }
    public class Test
    {
        public Telemetry Telemetry { get; set; }
    }
}
