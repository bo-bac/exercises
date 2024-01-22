using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Core;

internal class Producer : IProducer
{
    private const int RANDOM_COUNT = 16;

    static readonly Random s_random = new();
    static readonly Func<AddressFamily, ITcpClient> s_socketFactory = (AddressFamily af) =>
    {
        var socket = new Socket(af, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true,
        };
        return new TcpClientAdapter(socket);
    };

    private readonly Db _db;
    private readonly ILogger<Producer> _logger;
    private readonly Options _options;
    private readonly ConnectionFactory _connectionFactory;

    private readonly MessageBox _messagesToPublish = new();
    private readonly DeadLetterQueue _failedMessages = new();

    private int _counter;

    public Producer(
        ILogger<Producer> logger,
        IOptions<Options> options,
        Db db)
    {
        _logger = logger;
        _options = options.Value;
        _db = db;

        
        _connectionFactory = new ConnectionFactory
        {
            HostName = _options.BrokerHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(_options.NetworkRecoveryInterval),
            SocketFactory = s_socketFactory
        };
    }

    public Summary GetProducedSummary()
    {
        return new Summary
        {
            Hashes = _db.Hashs
                .GroupBy(h => h.Date)
                .Select(h => new SummaryItem
                {
                    Date = h.Key,
                    Count = (ulong)h.Count()
                })
                .ToArray()
        };
    }

    public async Task Produce()
    {
        var messages = GenerateHashes();
        await Publish(messages);
    }

    private IEnumerable<string> GenerateHashes()
    {
        using SHA1 sha1 = SHA1.Create();
        for (var i = 0; i < _options.HashCount; i++)
        {
            byte[] sourceBytes = RandomNumberGenerator.GetBytes(RANDOM_COUNT);
            byte[] hashBytes = sha1.ComputeHash(sourceBytes);

            yield return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
        }
    }

    private string GetKey(IConnection connection, IChannel channel) => $"{connection.ClientProvidedName}-{channel.ChannelNumber}";

    private async Task Publish(IEnumerable<string> messages)
    {
        var publishTasks = new List<Task>();
        var watch = Stopwatch.StartNew();

        var publishConnections = new List<IConnection>();
        for (int i = 0; i < _options.XPublishers; i++)
        {
            IConnection publishConnection = await _connectionFactory.CreateConnectionAsync($"{_options.AppId}-PRODUCE-{i}");
            publishConnections.Add(publishConnection);
        }

        try
        {
            await Parallel.ForEachAsync(messages.Chunk(_options.BatchSize), (batch, ct) =>
            {
                int idx = s_random.Next(publishConnections.Count);
                IConnection connection = publishConnections[idx];

                publishTasks.Add(Task.Run(async () =>
                {
                    using IChannel channel = await connection.CreateChannelAsync();
                    channel.ContinuationTimeout = TimeSpan.FromSeconds(_options.ContinuationTimeout);
                    channel.BasicAcks += (object? sender, BasicAckEventArgs e) => Channel_BasicAcks(new { Channel = sender, Session = connection }, e);
                    channel.BasicNacks += (object? sender, BasicNackEventArgs e) => Channel_BasicNacks(new { Channel = sender, Session = connection }, e);

                    await channel.ConfirmSelectAsync();

                    foreach (var message in batch)
                    {
                        ReadOnlyMemory<byte> body = Encoding.UTF8.GetBytes(message);
                        var properties = new BasicProperties
                        {
                            AppId = _options.AppId,
                            Persistent = true,
                            MessageId = message
                        };

                        var key = GetKey(connection, channel);
                        var seqno = channel.NextPublishSeqNo;
                        _messagesToPublish.AddMessage(key, seqno, message);

                        await channel.BasicPublishAsync(
                            exchange: string.Empty,
                            routingKey: _options.QueueName,
                            basicProperties: properties,
                            body: body,
                            mandatory: true);
                    }

                    await channel.WaitForConfirmsOrDieAsync();

                    _logger.LogDebug("Channel {number} done publishing and waiting for confirms", channel.ChannelNumber);
                }));

                return ValueTask.CompletedTask;
            });

            await Task.WhenAll(publishTasks.ToArray());
            watch.Stop();

            _logger.LogInformation($"Total messages sent: {_counter} for {watch.ElapsedMilliseconds} ms by {_options.XPublishers} connections");
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }
        finally
        {
            foreach (IConnection c in publishConnections)
            {
                _logger.LogDebug("Closing connection: {0}", c.ClientProvidedName);

                await c.CloseAsync();
            }
        }

        // todo: how to deel with DLQ
        if (_failedMessages.Any())
        {
            await Publish(_failedMessages.Get(_options.BatchSize));
        }
    }

    private void Channel_BasicAcks(object? sender, BasicAckEventArgs e)
    {
        dynamic? s = sender;
        if (s?.Channel is IChannel channel && s?.Session is IConnection connection)
        {
            var key = GetKey(connection, channel);
            var seqno = e.DeliveryTag;
            if (_messagesToPublish.TryGetMessage(key, seqno, out string? message))
            {
                var removed = _messagesToPublish.RemovedMessage(key, seqno, e.Multiple);
                Interlocked.Add(ref _counter, removed);
            }

            _logger.LogDebug(
                $"Channel[{channel.ChannelNumber}]: Message '{message}' with delivery tag '{e.DeliveryTag}' ack-ed, multiple is {e.Multiple}."
            );
        }
    }

    private void Channel_BasicNacks(object? sender, BasicNackEventArgs e)
    {
        dynamic? s = sender;
        if (s?.Channel is IChannel channel && s?.Session is IConnection connection)
        {
            var key = GetKey(connection, channel);
            var seqno = e.DeliveryTag;
            if (_messagesToPublish.TryGetMessage(key, seqno, out string? message))
            {
                _messagesToPublish.RemovedMessage(key, seqno, e.Multiple);
                _failedMessages.Add(message);
            }

            _logger.LogWarning(
                $"Message '{message}' with delivery tag '{e.DeliveryTag}' nack-ed, multiple is {e.Multiple}."
            );
        }
    }
}

internal class MessageBox
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<ulong, string>> _box = new();

    public int RemovedMessage(string key, ulong seqno, bool multiple)
    {
        int res = 0;
        void remove(ulong key, ConcurrentDictionary<ulong, string> dic)
        {
            if (dic.TryRemove(key, out _)) res++;
        }

        if (_box.TryGetValue(key, out var seq))
        {
            if (multiple)
            {
                var confirmed = seq.Where(k => k.Key <= seqno);
                foreach (var entry in confirmed)
                {
                    remove(entry.Key, seq);
                }
            }
            else
            {
                remove(seqno, seq);
            }
        }

        return res;
    }

    public bool AddMessage(string key, ulong seqno, string message) =>
        _box.GetOrAdd(key, new ConcurrentDictionary<ulong, string>()).TryAdd(seqno, message);


    public bool TryGetMessage(string key, ulong seqno, [NotNullWhen(true)] out string? message)
    {
        message = default;
        return _box.TryGetValue(key, out var seq) && seq.TryGetValue(seqno, out message);
    }
}

internal class DeadLetterQueue
{
    private readonly ConcurrentQueue<string> _queue = new();

    public void Add(string message)
    {
        _queue.Enqueue(message);
    }

    public bool Any() => _queue.Any();

    public IEnumerable<string> Get(int size)
    {
        for (var i = 0; i < size; i++)
        {
            if (_queue.TryDequeue(out string? message) && message is not null)
            {
                yield return message;
            }
        }
    }
}