using SdrHost.Messages;

namespace SdrHost.ControlItems.ReceiverState;

public record StartParameter(
    DataMode DataMode = DataMode.Real,
    CaptureMode CaptureMode = CaptureMode.Fifo,
    byte N = 1
);

public sealed class StartReceiverControlItemRequest : ControlMessage
{
    public const ushort CODE = 0x0018;
    private const byte TYPE = (byte)MessageTypes.HostToTarget.Set;

    public StartReceiverControlItemRequest(StartParameter? parameter = default) : base(CODE, TYPE)
    {
        parameter = new StartParameter();

        // validate
        if (parameter.CaptureMode == CaptureMode.Fifo && (parameter.N < 1 || parameter.N > 255))
        {
            throw new ArgumentOutOfRangeException($"N should be in 1 to 255 range.");
        }

        var n = (byte)(parameter.CaptureMode == CaptureMode.Fifo
                ? parameter.N
                : 0x00);

        Parameter =
        [
            (byte)parameter.DataMode,
            (byte)DataCapturingControl.Run,
            (byte)parameter.CaptureMode,
            n
        ];
    }
}