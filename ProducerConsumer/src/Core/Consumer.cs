using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace Core;

internal sealed class Consumer : IConsumer
{
    static readonly Func<AddressFamily, ITcpClient> s_socketFactory = (AddressFamily af) =>
    {
        var socket = new Socket(af, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true,
        };
        return new TcpClientAdapter(socket);
    };

    private IConnection? _connection;
    private readonly List<(IChannel Channel, AsyncEventingBasicConsumer Listener)> _listeners = new();

    private readonly IDbContextFactory<Db> _dbFactory;
    private readonly ILogger<Producer> _logger;
    private readonly Options _options;
    private readonly ConnectionFactory _connectionFactory;

    private readonly int[] _counters;
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

        _counters = new int[_options.XConsumers];

        _connectionFactory = new ConnectionFactory
        {
            HostName = _options.BrokerHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(_options.NetworkRecoveryInterval),
            ConsumerDispatchConcurrency = _options.XConsumers,

            DispatchConsumersAsync = true,
            SocketFactory = s_socketFactory
        };
    }

    public async Task StartConsume()
    {
        var name = $"{_options.AppId}-CONSUME-0";
        _logger.LogInformation("Opening connection: {name}", name);
        _connection = await _connectionFactory.CreateConnectionAsync(name);

        for (int i = 0; i < _options.XConsumers; i++)
        {
            var channel = await CreateChannel(_connection);
            var listener = new AsyncEventingBasicConsumer(channel);
            listener.Received += Consumer_Received;

            await channel.BasicConsumeAsync(queue: _options.QueueName, autoAck: false, consumer: listener);

            _listeners.Add((channel, listener));
        }
    }

    public async Task StopConsume()
    {
        StringBuilder exceptions = new();

        foreach (var c in _listeners)
        {
            try
            {
                await c.Channel.CloseAsync();
            }
            catch(Exception e)
            {
                exceptions.AppendLine(e.Message);
            }
            finally
            {
                c.Channel.Dispose();
            }
        }

        _listeners.Clear();

        if (_connection is not null)
        {
            _logger.LogInformation("Closing connection: {name}", _connection.ClientProvidedName);

            try
            {
                await _connection.CloseAsync();
            }
            catch (Exception e)
            {
                exceptions.AppendLine(e.Message);
            }
            finally
            {
                _connection.Dispose();
            }            
        }

        if (exceptions.Length > 0)
        {
            _logger.LogError(exceptions.ToString());
        }        
    }

    public string GetStatistics()
    {
        return _counters
            .Select((v, i) => $" {i + 1}:{v}")
            .Aggregate($"Consumed {_counters.Sum()} for {ms}ms/{ms / 60000}m by -", (s, next) => s + next);
    }

    public async ValueTask DisposeAsync()
    {
        await StopConsume();
    }

    private async Task<IChannel> CreateChannel(IConnection connection)
    {
        var channel = await connection.CreateChannelAsync();
        channel.ContinuationTimeout = TimeSpan.FromSeconds(_options.ContinuationTimeout);

        // prefetchCount = 1
        // For example in a situation with two workers, when all odd messages are heavy and even messages are light, one worker will be constantly busy and the other one will do hardly any work.
        // This tells RabbitMQ not to give more than one message to a worker at a time. Or, in other words, don't dispatch a new message to a worker until it has processed and acknowledged the previous one. Instead, it will dispatch it to the next worker that is not still busy.
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

        //await channel.ExchangeDeclareAsync(exchange: ExchangeName,
        //        type: ExchangeType.Direct, passive: false, durable: false, autoDelete: false, arguments: null);

        await channel.QueueDeclareAsync(
            queue: _options.QueueName,
            passive: true,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        //await channel.QueueBindAsync(queue: QueueName, exchange: ExchangeName, routingKey: RoutingKey, arguments: null);

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

    private async Task Consumer_Received(object? sender, BasicDeliverEventArgs e)
    {
        var sw = new Stopwatch();
        sw.Start();


        if (sender is IBasicConsumer consumer)
        {
            int id = consumer.Channel.ChannelNumber;

            _logger.LogDebug("Consumer {id}: processing started...", id);

            // do some time consuming processing here
            var body = e.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                SaveToDb(message);
                await consumer.Channel.BasicAckAsync(e.DeliveryTag, false);

                _logger.LogDebug("Consumed[{ch}:{thread}]: {message} tag:{tag}", id, Environment.CurrentManagedThreadId, message, e.DeliveryTag);
                _counters[id - 1]++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                await consumer.Channel.BasicNackAsync(e.DeliveryTag, false, requeue: true);
            }

            _logger.LogDebug("Consumer {id}: processing ended.", id);
        }


        sw.Stop();
        Interlocked.Add(ref ms, sw.ElapsedMilliseconds);
    }
}
