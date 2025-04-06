using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaBus
{
    internal class Bootstrapper : BackgroundService, IAsyncDisposable
    {
        private readonly IEnumerable<IProcessor> _processors;
        private CancellationTokenSource? _cts;
        private bool _disposed;

        public Bootstrapper(IEnumerable<IProcessor> startUps)
        {
            _processors = startUps;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_cts != null)
                return;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

            //If cancelled, dispose of all processors
            _cts.Token.Register(() =>
            {
                try
                {
                    foreach (var processor in _processors)
                        processor.Dispose();
                }
                catch
                { }
            });

            //Start all processors
            _disposed = false;
            foreach (var processor in _processors)
            {
                try
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    await processor.Start(_cts.Token);
                }
                catch { }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts?.Cancel();
            await base.StopAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }

        public override void Dispose()
        {
            base.Dispose();
            if (!_disposed)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
                _disposed = true;
            }
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return new ValueTask(Task.CompletedTask);
        }
    }
}