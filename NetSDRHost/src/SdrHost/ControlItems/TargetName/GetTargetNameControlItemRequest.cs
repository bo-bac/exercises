using SdrHost.Messages;

namespace SdrHost.ControlItems.TargetName;


public sealed class GetTargetNameControlItemRequest : ControlMessage
{
    private const byte TYPE = (byte)MessageTypes.HostToTarget.Get;
    public const ushort CODE = 0x0001;

    public GetTargetNameControlItemRequest() : base(CODE, TYPE)
    {
    }
}