using Microsoft.AspNetCore.SignalR.Client;
using rF2SMMonitor;
using rF2SMMonitor.rFactor2Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace TelemetryApp
{
    class Program
    {

        [STAThread]
        static void Main(string[] args)
        {
            //    // Set Invariant Culture for all threads as default.
            //    CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            //    CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;            

            //    var manager = new ApplicationManager();
            //    var token = new CancellationTokenSource().Token;
            //    Task.Factory.StartNew(async (x) =>
            //    {
            //        await manager.Run();
            //    }, TaskContinuationOptions.LongRunning, token);
        }
        
    }
}

