using SdrHost.Messages;

namespace SdrHost.ControlItems.ReceiverFrequency;

public sealed class FrequencyControlItemResponse : ControlMessage
{
    private const byte TYPE = (byte)MessageTypes.TargetToHost.Response;
    public const ushort CODE = 0x0020;

    public FrequencyChannel Channel { get; private init; }
    public ulong Frequency { get; private init; }

    public FrequencyControlItemResponse(ControlMessage message) : base(message.Code, message.Type, message.Parameter)
    {
        if (IsNAK) return;

        if (Type != TYPE && Type != (byte)MessageTypes.TargetToHost.Unsolicited)
        {
            throw new ArgumentException("Invalid Message Type.");
        }

        if (Code != CODE)
        {
            throw new ArgumentException("Invalid Control Code.");
        }

        if (Parameter is null)
        {
            throw new ArgumentException("Message Parameter should not be null.");
        }

        if (Parameter.Length != 6)
        {
            throw new ArgumentException("Invalid Parameter length. Expected 6 bytes.");
        }

        // Extract and validate Channel
        byte channel = Parameter[0];
        if (!Enum.IsDefined(typeof(FrequencyChannel), channel))
        {
            throw new ArgumentOutOfRangeException("Invalid Channel value.");
        }

        Channel = (FrequencyChannel)channel;

        // Read the 5-byte frequency (Little-Endian → Least Significant Byte first)
        Frequency =
            Parameter[1] |
            (ulong)Parameter[2] << 8 |
            (ulong)Parameter[3] << 16 |
            (ulong)Parameter[4] << 24 |
            (ulong)Parameter[5] << 32;
    }
}