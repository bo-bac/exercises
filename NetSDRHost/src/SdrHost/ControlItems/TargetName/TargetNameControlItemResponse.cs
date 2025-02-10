using System.Text;
using SdrHost.Messages;

namespace SdrHost.ControlItems.TargetName;

public sealed class TargetNameControlItemResponse : ControlMessage
{
    private const byte TYPE = (byte)MessageTypes.TargetToHost.Response;
    public const ushort CODE = 0x0001;

    public string? Name { get; private init; }

    public TargetNameControlItemResponse(ControlMessage message) : base(message.Code, message.Type, message.Parameter)
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

        Name = Encoding.UTF8.GetString(Parameter);
    }
}