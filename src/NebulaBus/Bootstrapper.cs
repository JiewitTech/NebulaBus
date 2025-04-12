using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NebulaBus.Scheduler;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaBus
{
    internal class Bootstrapper : BackgroundService, IAsyncDisposable
    {
        private CancellationTokenSource? _cts;
        private bool _disposed;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Bootstrapper> _logger;

        public Bootstrapper(IServiceProvider serviceProvider, ILogger<Bootstrapper> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_cts != null)
                return;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

            var _processors = _serviceProvider.GetServices<IProcessor>();
            var _delayMessageScheduler = _serviceProvider.GetService<IDelayMessageScheduler>();

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
                    await processor.Start(_cts.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Start Processor:{processor.GetType().Name} Failed");
                }
            }

            //Start Store Scheduler
            await _delayMessageScheduler!.StartSchedule(_cts.Token).ConfigureAwait(false);
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