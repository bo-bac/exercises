using SdrHost.Messages;

namespace SdrHost.ControlItems.ReceiverFrequency;

public sealed class GetFrequencyControlItemRequest : ControlMessage
{
    private const byte TYPE = (byte)MessageTypes.HostToTarget.Get;
    public const ushort CODE = 0x0020;

    public GetFrequencyControlItemRequest(FrequencyChannel channel) : base(CODE, TYPE)
    {
        // validate
        if (channel == FrequencyChannel.AllChannels)
        {
            throw new ArgumentOutOfRangeException($"AllChannels value for Channel parameter is not valid. You should specify chanel.");
        }

        Parameter = [
            (byte)channel
        ];
    }
}