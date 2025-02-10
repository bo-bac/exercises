using BenchmarkDotNet.Attributes;
using SdrHost.ControlItems.ReceiverFrequency;
using SdrHost.Messages;

namespace Benchmark;

public class ControlMessageBenchmark
{
    private readonly ControlMessage _controlMessage;
    private readonly Real16FifoDataMessage _fifoMessage;
    private readonly MemoryStream _stream;

    public ControlMessageBenchmark()
    {
        _controlMessage = new ControlMessage(
            code: GetFrequencyControlItemRequest.CODE,
            type: (byte)MessageTypes.HostToTarget.Get,
            parameter: [0x02]
        );
        _fifoMessage = new Real16FifoDataMessage
        {
            SeqN = 4,
            BigPackets = false,
            Type = (byte)MessageTypes.TargetToHost.Data0,
            Data = new byte[512]
        };
        _stream = new MemoryStream();
    }

    [Benchmark]
    public byte[] SerializeControlMessage() => _controlMessage.Serialize();

    [Benchmark]
    public async Task<ControlMessage?> DeserializeControlMessage()
    {
        _stream.Seek(0, SeekOrigin.Begin);
        return await ControlMessage.Deserialize(_stream, CancellationToken.None);
    }

    [Benchmark]
    public byte[] SerializeFifoMessage() => _fifoMessage.Serialize();

    [Benchmark]
    public async Task<Real16FifoDataMessage?> DeserializeFifoMessage()
    {
        _stream.Seek(0, SeekOrigin.Begin);
        return await Real16FifoDataMessage.Deserialize(_stream, CancellationToken.None);
    }
}
