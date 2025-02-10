using SdrHost.Messages;

namespace SdrHost.ControlItems.ReceiverState;

public sealed class StopReceiverControlItemRequest : ControlMessage
{
    public const ushort CODE = 0x0018;
    private const byte TYPE = (byte)MessageTypes.HostToTarget.Set;

    public StopReceiverControlItemRequest() : base(CODE, TYPE)
    {
        Parameter =
        [
            0x00,
            (byte)DataCapturingControl.Idle,
            0x00,
            0x00
        ];
    }
}