using SdrHost.Messages;

namespace Tests.Messages;

public class MessageTest
{
    public static TheoryData<byte[], ControlMessage> Messages => new()
    {
        // 4.2.3 Receiver Frequency
        {
            // get the current NCO frequency for channel 2
            [0x05,0x20,  0x20,0x00, 0x02],
            new ControlMessage(
                code: 0x0020,
                type: (byte)MessageTypes.HostToTarget.Get,
                parameter: [0x02]
            )
        },
        {
            //response
            [0x0A,0x00,  0x20,0x00, 0x02, 0x90,0xC6,0xD5,0x00, 0x00],
            new ControlMessage(
                code: 0x0020,
                type: (byte)MessageTypes.TargetToHost.Response,
                parameter: [0x02, 0x90,0xC6,0xD5,0x00, 0x00]
            )
        },

        //4.2.1 Receiver State
        {
            //start (Real 16Bit FIFO N=1)
            [0x08,0x00, 0x18,0x00, 0x00,0x02,0x01,0x01],
            new ControlMessage(
                code: 0x0018,
                type: (byte)MessageTypes.HostToTarget.Set,
                parameter: [0x00, 0x02, 0x01, 0x01]
            )
        },
        { 
            //stop
            [0x08,0x00, 0x18,0x00, 0x00,0x01,0x00,0x00],
            new ControlMessage(
                code: 0x0018,
                type: (byte)MessageTypes.HostToTarget.Set,
                parameter: [0x00, 0x01, 0x00, 0x00]
            )
        },

    };

    [Theory()]
    [MemberData(nameof(Messages))]
    public async Task ControlMessage_Should_Serialize_And_Deserialize(byte[] buffer, ControlMessage expected)
    {
        //Arrange
        using var mockStream = new MemoryStream(buffer);

        //Act
        var actual = await ControlMessage.Deserialize(mockStream);
        var actualBuffer = actual.Serialize();

        //Assert
        Assert.Equal(expected.GetHeader(), actual.GetHeader());
        Assert.Equal(expected.Code, actual.Code);
        Assert.Equal(expected.Parameter, actual.Parameter);
        Assert.Equal(buffer, actualBuffer);
    }

    public static TheoryData<byte[], bool> NAKTestMessages => new()
    {
        {
            [0x05,0x20,  0x20,0x00, 0x02],
            false
        },
        {
            [0x04,0x20,  0x10,0x00],
            false
        },
        {
            [0x02, 0x00],
            true
        }
    };
    [Theory()]
    [MemberData(nameof(NAKTestMessages))]
    public async Task Empty_ControlMessage_With_Length_of_2_and_Type_0_Is_NAK(byte[] buffer, bool expected)
    {
        //Arrange
        using var mockStream = new MemoryStream(buffer);

        //Act
        var actual = await ControlMessage.Deserialize(mockStream);

        //Assert
        Assert.Equal(expected, actual.IsNAK);
    }

    [Fact]
    public void Empty_ControlMessage_Is_NAK()
    {
        //Arrange
        var objectNak = new ControlMessage(
            code: 0,
            type: 0,
            parameter: null
        );

        //Act

        //Assert
        Assert.True(objectNak.IsNAK);
    }
}
