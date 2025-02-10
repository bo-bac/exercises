using SdrHost.Messages;

namespace SdrHost.ControlItems.ReceiverFrequency;

public record SetFrequencyParameter(
    FrequencyChannel Channel = FrequencyChannel.Channel1,
    ulong Frequency = 14010000,
    byte N = 1
);

public sealed class SetFrequencyControlItemRequest : ControlMessage
{
    private const byte TYPE = (byte)MessageTypes.HostToTarget.Set;
    public const ushort CODE = 0x0020;

    public SetFrequencyControlItemRequest(SetFrequencyParameter parameter) : base(CODE, TYPE)
    {
        Parameter =
        [
            // First byte: Channel
            (byte)parameter.Channel,
            // Next 5 bytes: Frequency (Little-Endian)
            (byte)(parameter.Frequency & 0xFF),
            (byte)(parameter.Frequency >> 8 & 0xFF),
            (byte)(parameter.Frequency >> 16 & 0xFF),
            (byte)(parameter.Frequency >> 24 & 0xFF),
            (byte)(parameter.Frequency >> 32 & 0xFF),
        ];
    }
}