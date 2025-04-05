using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaBus
{
    internal class Bootstrapper : BackgroundService, IAsyncDisposable
    {
        public Bootstrapper(IStartUp[] startUps)
        {
        }

        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            throw new System.NotImplementedException();
        }
    }
}