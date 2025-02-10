using SdrHost.Messages;

namespace Tests;

public class Real16FifoDataMessageTest
{
    public static TheoryData<byte[], Real16FifoDataMessage> Messages => new()
    {

        // 4.5.1.1 Real 16 Bit FIFO Data

        // large MTU packet            
        {
            (new byte[]{ 0x04,0x84,  0x02,0x00 }).Concat(new byte[1024]).ToArray(),
            new Real16FifoDataMessage
            {
                SeqN = 2,
                BigPackets = true,
                Type = (byte)MessageTypes.TargetToHost.Data0,
                Data = new byte[1024]
            }
        },
        // small MTU packet
        {
            (new byte[]{ 0x04,0x82,  0x04,0x00 }).Concat(new byte[512]).ToArray(),
            new Real16FifoDataMessage
            {
                SeqN = 4,
                BigPackets = false,
                Type = (byte)MessageTypes.TargetToHost.Data0,
                Data = new byte[512]
            }
        }
    };

    [Theory()]
    [MemberData(nameof(Messages))]
    public async Task Real16FifoDataMessage_Should_Serialize_And_Deserialize(byte[] buffer, Real16FifoDataMessage expected)
    {
        //Arrange
        using var mockStream = new MemoryStream(buffer);

        //Act
        var actual = await Real16FifoDataMessage.Deserialize(mockStream);
        var actualBuffer = actual.Serialize();

        //Assert
        Assert.Equal(expected.GetHeader(), actual.GetHeader());
        Assert.Equal(expected.SeqN, actual.SeqN);
        Assert.Equal(expected.BigPackets, actual.BigPackets);
        Assert.Equal(expected.Type, actual.Type);
        Assert.Equal(expected.Data, actual.Data);
        Assert.Equal(buffer, actualBuffer);
    }
}
