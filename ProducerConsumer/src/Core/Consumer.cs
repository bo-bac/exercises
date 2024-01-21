using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Diagnostics;
using System.Text;

namespace Core;

internal sealed class Consumer : IConsumer
{
    private bool _disposed;
    private IConnection? _connection;
    private readonly List<(IModel Channel, EventingBasicConsumer Listener)> _listeners = new();

    private readonly IDbContextFactory<Db> _dbFactory;
    private readonly ILogger<Producer> _logger;
    private readonly Options _options;
    private readonly ConnectionFactory _connectionFactory;

    private readonly int[] _counter;
    private long ms;

    public Consumer(
        ILogger<Producer> logger,
        IOptions<Options> options,
        IDbContextFactory<Db> dbFactory
        )
    {
        _logger = logger;
        _options = options.Value;
        _dbFactory = dbFactory;

        _counter = new int[_options.XConsumers];

        _connectionFactory = new ConnectionFactory
        {
            HostName = _options.BrokerHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(_options.NetworkRecoveryInterval),
            ConsumerDispatchConcurrency = _options.XConsumers
        };
    }

    public void StartConsume()
    {
        var name = $"{_options.AppId}-CONSUME-0";
        _logger.LogInformation("Opening connection: {name}", name);
        _connection = CreateConnection(name);

        for (int i = 0; i < _options.XConsumers; i++)
        {
            var channel = CreateChannel(_connection);
            var listener = new EventingBasicConsumer(channel);
            listener.Received += Consumer_Received;

            channel.BasicConsume(queue: _options.QueueName, autoAck: false, consumer: listener);

            _listeners.Add((channel, listener));
        }
    }

    public void StopConsume()
    {
        foreach (var c in _listeners)
        {
            c.Channel.Close();
            c.Channel.Dispose();
        }

        _listeners.Clear();

        if (_connection is not null)
        {
            _logger.LogInformation("Closing connection: {name}", _connection.ClientProvidedName);
            _connection.Close();
            _connection.Dispose();
        }
    }

    public string GetStatistics()
    {
        return _counter
            .Select((v, i) => $" {i + 1}:{v}")
            .Aggregate($"Consumed {_counter.Sum()} for {ms}ms/{ms/60000}m by -", (s, next) => s + next);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            StopConsume();
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }
        finally
        {
            _disposed = true;
        }
    }

    private IConnection CreateConnection(string name)
    {
        IConnection connection = _connectionFactory.CreateConnection(name);

        return connection;
    }

    private IModel CreateChannel(IConnection connection)
    {
        var channel = connection.CreateModel();
        channel.ContinuationTimeout = TimeSpan.FromSeconds(_options.ContinuationTimeout);

        channel.QueueDeclare(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // prefetchCount = 1
        // For example in a situation with two workers, when all odd messages are heavy and even messages are light, one worker will be constantly busy and the other one will do hardly any work.
        // This tells RabbitMQ not to give more than one message to a worker at a time. Or, in other words, don't dispatch a new message to a worker until it has processed and acknowledged the previous one. Instead, it will dispatch it to the next worker that is not still busy.
        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        return channel;
    }

    private void SaveToDb(string hash)
    {
        using var db = _dbFactory.CreateDbContext();
        db.Hashs.Add(new Hash
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Sha1 = hash
        });

        db.SaveChanges();
    }

    private void Consumer_Received(object? sender, BasicDeliverEventArgs e)
    {
        var sw = new Stopwatch();
        sw.Start();


        if (sender is IBasicConsumer consumer)
        {
            int id = consumer.Model.ChannelNumber;

            _logger.LogDebug("Consumer {id}: processing started...", id);

            // do some time consuming processing here
            var body = e.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                SaveToDb(message);
                consumer.Model.BasicAck(e.DeliveryTag, false);

                _logger.LogDebug("Consumed[{ch}:{thread}]: {message} tag:{tag}", id, Environment.CurrentManagedThreadId, message, e.DeliveryTag);
                _counter[id - 1]++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                consumer.Model.BasicNack(e.DeliveryTag, false, requeue:true);
            }

            _logger.LogDebug("Consumer {id}: processing ended.", id);
        }


        sw.Stop();
        Interlocked.Add(ref ms, sw.ElapsedMilliseconds);
    }
}
