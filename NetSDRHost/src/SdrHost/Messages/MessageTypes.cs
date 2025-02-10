namespace SdrHost.Messages;

public static class MessageTypes
{
    public enum HostToTarget : byte
    {
        Set = 0x00,
        Get = 0x01,
        GetRange = 0x02,
        Ack = 0x03,
        Data0 = 0x04,
        Data1 = 0x05,
        Data2 = 0x06,
        Data3 = 0x07,
    }

    public enum TargetToHost : byte
    {
        Response = 0x00,
        Unsolicited = 0x01,
        ResponseRange = 0x02,
        Ack = 0x03,
        Data0 = 0x04,
        Data1 = 0x05,
        Data2 = 0x06,
        Data3 = 0x07,
    }
}