using SdrHost.Messages;

namespace SdrHost.ControlItems.ReceiverState;

public enum DataCapturingControl : byte
{
    Nak = 0,
    Idle = 0x01,    //stops the UDP port and data capture
    Run = 0x02      //starts the UDP port NetSDR capturing data
}

public enum DataMode : byte
{
    Real = 0x00,    //real A/D sample data mode
    Complex = 0x01  //complex I/Q base band data mode
}

public enum CaptureMode : byte
{
    Contiguous16 = 0x00,    //16 bit Contiguous mode where the data is contiguously sent back to the Host.
    Contiguous24 = 0x80,    //24 bit Contiguous mode where the data is contiguously sent back to the Host.
    Fifo = 0x01,            //16 bit FIFO mode where N samples of data is captured in a FIFO then sent back to the Host.
    Pulse24 = 0x83,         //24 bit Hardware Triggered Pulse mode.(start/stop controlled by HW trigger input)(**Optional)
    Pulse16 = 0x03,         //16 bit Hardware Triggered Pulse mode.(start/stop controlled by HW trigger input)(**Optional)
}



public sealed class ReceiverStateControlItemResponse : ControlMessage
{
    public const ushort CODE = 0x0018;
    private const byte TYPE = (byte)MessageTypes.TargetToHost.Response;

    public DataCapturingControl DataCapturingControl { get; private init; }
    public DataMode DataMode { get; private init; }
    public CaptureMode CaptureMode { get; private init; }

    /// <summary>
    /// N specifying the number of 4096 16 bit data samples to capture in the FIFO mode
    /// </summary>
    public byte N { get; private init; }

    public bool IsCapturingData => DataCapturingControl == DataCapturingControl.Run;

    public ReceiverStateControlItemResponse(ControlMessage message) : base(message.Code, message.Type, message.Parameter)
    {
        if (IsNAK) return;

        if (Type != TYPE && Type != (byte)MessageTypes.TargetToHost.Unsolicited)
        {
            throw new ArgumentException("Invalid Message Type.");
        }

        if (Code != CODE)
        {
            throw new ArgumentException("Invalid Controll Item Code.");
        }

        if (Parameter is null)
        {
            throw new ArgumentException("Message Parameter should not be null.");
        }

        if (Parameter.Length != 4)
        {
            throw new ArgumentException("Invalid Parameter length. Expected 4 bytes.");
        }

        // Extract and validate DataMode
        byte dataMode = Parameter[0];
        if (!Enum.IsDefined(typeof(DataMode), dataMode))
        {
            throw new ArgumentOutOfRangeException("Invalid DataMode value.");
        }

        DataMode = (DataMode)dataMode;

        // Extract and validate DataCapturingControl
        byte control = Parameter[1];
        if (!Enum.IsDefined(typeof(DataCapturingControl), control))
        {
            throw new ArgumentOutOfRangeException("Invalid DataMode value.");
        }

        DataCapturingControl = (DataCapturingControl)control;

        // Extract and validate DataMode
        byte captureMode = Parameter[2];
        if (!Enum.IsDefined(typeof(CaptureMode), captureMode))
        {
            throw new ArgumentOutOfRangeException("Invalid CaptureMode value.");
        }

        CaptureMode = (CaptureMode)captureMode;

        // Extract N
        N = Parameter[3];
    }
}