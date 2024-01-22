using Core;

namespace Consumer
{
    public sealed class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConsumer _consumer;

        public Worker(
            ILogger<Worker> logger,
            IConsumer consumer)
        {
            _logger = logger;
            _consumer = consumer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            await _consumer.StartConsume();

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation(_consumer.GetStatistics());
                await Task.Delay(10000, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _consumer.StopConsume();
            await base.StopAsync(cancellationToken);
        }
    }
}
