using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading;
using TelemetryApp;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using rF2SMMonitor.rFactor2Data;
using rF2SMMonitor;

namespace TelemetryAppWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var webHost = BuildWebHost(args);
            IServiceCollection services = new ServiceCollection();

            services.AddSingleton<ApplicationManager>();
            services.AddSingleton<IMappedDoubleBuffer<rF2Telemetry>>(
                new MappedDoubleBuffer<rF2Telemetry>(
                    rFactor2Constants.MM_TELEMETRY_FILE_NAME1,
                    rFactor2Constants.MM_TELEMETRY_FILE_NAME2, 
                    rFactor2Constants.MM_TELEMETRY_FILE_ACCESS_MUTEX)
            );

            services.AddSingleton<IMappedDoubleBuffer<rF2Scoring>>(
                new MappedDoubleBuffer<rF2Scoring>(
                    rFactor2Constants.MM_SCORING_FILE_NAME1,
                    rFactor2Constants.MM_SCORING_FILE_NAME2,
                    rFactor2Constants.MM_SCORING_FILE_ACCESS_MUTEX)
            );

            services.AddSingleton<IConnectionManager, ConnectionManager>();

            var serviceProvider = services.BuildServiceProvider();

            var manager = serviceProvider.GetService<ApplicationManager>();
            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            Task.Factory.StartNew(async (x) =>
            {
                await manager.Run(token);
            }, TaskContinuationOptions.LongRunning, token);

            webHost.Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}
