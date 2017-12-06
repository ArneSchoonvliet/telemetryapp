using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TelemetryApp;

namespace TelemetryAppWeb.Hubs
{
    public class Chat : Hub
    {
        public Task Send(Test message)
        {
            return Clients.All.InvokeAsync("Send", message);
        }
    }
}
