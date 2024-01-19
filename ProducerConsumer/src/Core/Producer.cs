using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace Core;

internal class Producer : IProducer
{
    private const int RANDOM_COUNT = 16;
    private const int HASH_COUNT = 40000;

    private readonly Db _db;
    private readonly ILogger<Producer> _logger;
    private readonly Options _options;

    private readonly ConnectionFactory _connectionFactory;

    private readonly MessageBox _messagesToPublish = new();
    private readonly DeadLetterQueue _failedMessages = new();

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
            NetworkRecoveryInterval = TimeSpan.FromSeconds(_options.NetworkRecoveryInterval)
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

    public void Produce()
    {
        var messages = GenerateHashes();
        Publish(messages);
    }

    private IEnumerable<string> GenerateHashes()
    {
        using SHA1 sha1 = SHA1.Create();
        for (var i = 0; i < HASH_COUNT; i++)
        {
            byte[] sourceBytes = RandomNumberGenerator.GetBytes(RANDOM_COUNT);
            byte[] hashBytes = sha1.ComputeHash(sourceBytes);

            yield return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
        }
    }

    private void Publish(IEnumerable<string> messages)
    {
        var watch = Stopwatch.StartNew();

        var name = $"{_options.AppId}-PRODUCE-0";
        using var connection = _connectionFactory.CreateConnection(name);

        using var channel = connection.CreateModel();
        channel.ContinuationTimeout = TimeSpan.FromSeconds(_options.ContinuationTimeout);
        channel.BasicAcks += Channel_BasicAcks;
        channel.BasicNacks += Channel_BasicNacks;

        channel.QueueDeclare(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // NextPublishSeqNo will be set to 1
        channel.ConfirmSelect();

        foreach (var batch in messages.Chunk(_options.BatchSize))
        {
            PublishBatch(channel.NextPublishSeqNo, batch, channel.CreateBasicProperties(), channel.CreateBasicPublishBatch());

            channel.WaitForConfirmsOrDie();
        }

        //await WaitUntilConditionMet(
        //    () => Task.FromResult(_messagesToPublish.IsEmpty),
        //    TimeOut,
        //    "All messages could not be confirmed in 60 seconds"
        //);

        watch.Stop();
        _logger.LogInformation($"Total messages sent: {channel.NextPublishSeqNo - 1} for {watch.Elapsed.TotalMilliseconds} ms");


        // todo: how to deel with DLQ
        if (_failedMessages.Any())
        {
            Publish(_failedMessages.Get(_options.BatchSize));
        }
    }

    private void PublishBatch(
        ulong secNo,
        IReadOnlyCollection<string> messages,
        IBasicProperties properties,
        IBasicPublishBatch batch)
    {
        properties.AppId = _options.AppId;
        properties.Persistent = true;
        //properties.Headers = envelope.Metadata;
        //properties.ContentType = "application/json";
        //properties.Type = TypeMapper.GetTypeName(envelope.Message.GetType());
        //properties.MessageId = envelope.Message.MessageId.ToString();

        foreach (var message in messages)
        {
            ReadOnlyMemory<byte> body = Encoding.UTF8.GetBytes(message);

            batch.Add(
                exchange: string.Empty,
                routingKey: _options.QueueName,
                mandatory: true,
                properties: properties,
                body: body
            );

            _messagesToPublish.AddMessage(secNo++, message);
        }

        // Publish the batch of messages in a single transaction,
        // After publishing publish messages sequence number will be incremented.
        // internally will assign NextPublishSeqNo for each message and them to pendingDeliveryTags collection                
        batch.Publish();
    }

    private void Channel_BasicAcks(object? sender, BasicAckEventArgs e)
    {
        if (_messagesToPublish.TryGetMessage(e.DeliveryTag, out string message))
        {
            _messagesToPublish.RemovedMessage(e.DeliveryTag, e.Multiple);
        }

        _logger.LogDebug(
            $"Message '{message}' with delivery tag '{e.DeliveryTag}' ack-ed, multiple is {e.Multiple}."
        );
    }

    private void Channel_BasicNacks(object? sender, BasicNackEventArgs e)
    {
        if (_messagesToPublish.TryGetMessage(e.DeliveryTag, out string message))
        {
            _messagesToPublish.RemovedMessage(e.DeliveryTag, e.Multiple);
            _failedMessages.Add(message);
        }

        _logger.LogWarning(
            $"Message '{message}' with delivery tag '{e.DeliveryTag}' nack-ed, multiple is {e.Multiple}."
        );
    }
}

internal class MessageBox
{
    private readonly ConcurrentDictionary<ulong, string> _box = new();

    public void RemovedMessage(ulong sequenceNumber, bool multiple)
    {
        if (multiple)
        {
            var confirmed = _box.Where(k => k.Key <= sequenceNumber);
            foreach (var entry in confirmed)
            {
                _box.TryRemove(entry.Key, out _);
            }
        }
        else
        {
            _box.TryRemove(sequenceNumber, out _);
        }
    }

    public bool AddMessage(ulong sequenceNumber, string message)
        => _box.TryAdd(sequenceNumber, message);

    public bool TryGetMessage(ulong sequenceNumber, [NotNullWhen(true)] out string message)
        => _box.TryGetValue(sequenceNumber, out message!);
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