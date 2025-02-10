using SdrHost.Messages;

namespace Tests.Messages;

public class HeaderTest
{
    public static TheoryData<byte[], Header> Headers => new()
    {
        { [0x04, 0x20], new Header((byte)MessageTypes.HostToTarget.Get, 4) },
        { [0x0B, 0x00], new Header((byte)MessageTypes.TargetToHost.Response, 11) },
        { [0x05, 0x20], new Header((byte)MessageTypes.HostToTarget.Get, 5) },
        { [0x05, 0x40], new Header((byte)MessageTypes.HostToTarget.GetRange, 5) },
        { [0x08, 0x00], new Header((byte)MessageTypes.TargetToHost.Response, 8) },
    };

    [Theory()]
    [MemberData(nameof(Headers))]
    public void Header_Should_Be_Serialized_And_Desirialized(byte[] buffer, Header expected)
    {
        //Arrange
        var word = BitConverter.ToUInt16(buffer, 0);

        //Act
        var actual = new Header(word);
        var actualWord = actual.ToUShort();
        var actualBuffer = actual.Serialize();

        //Assert
        Assert.Equal(expected.Type, actual.Type);
        Assert.Equal(expected.Length, actual.Length);
        Assert.Equal(word, actualWord);
        Assert.Equal(buffer, actualBuffer);
    }

    public static TheoryData<byte[], Header> DataItemHeaders => new()
    {
        //4.5.1.1 Real 16 Bit FIFO Data
        
        // large MTU packet
        { [0x04, 0x84], new Header((byte)MessageTypes.TargetToHost.Data0, 1028) },
        
        // small MTU packet
        { [0x04, 0x82], new Header((byte)MessageTypes.TargetToHost.Data0, 516) },
    };

    [Theory()]
    [MemberData(nameof(DataItemHeaders))]
    public void DataItem_Header_Should_Be_Serialized_And_Desirialized(byte[] buffer, Header expected)
    {
        //Arrange
        var word = BitConverter.ToUInt16(buffer, 0);

        //Act
        var actual = new Header(word);
        var actualWord = actual.ToUShort();
        var actualBuffer = actual.Serialize();

        //Assert
        Assert.Equal(expected.Type, actual.Type);
        Assert.Equal(expected.Length, actual.Length);
        Assert.Equal(word, actualWord);
        Assert.Equal(buffer, actualBuffer);
    }

    public static TheoryData<byte[], bool> DeserializedNAKTest => new()
    {
        { [0x05,0x20], false },
        { [0x04,0x20], false },
        { [0x02,0x00], true }
    };
    [Theory()]
    [MemberData(nameof(DeserializedNAKTest))]
    public async Task Deserialized_Header_With_Length_of_2_and_Type_0_Is_NAK(byte[] buffer, bool expected)
    {
        //Arrange
        using var mockStream = new MemoryStream(buffer);

        //Act
        var actual = await Header.Deserialize(mockStream);

        //Assert
        Assert.Equal(expected, actual?.IsNAK);
    }

    public static TheoryData<Header, bool> NAKTest => new()
    {
        { new Header(2), true },
        { new Header(Type:0, Length:2), true },
        { new Header(), false },
        { new Header(0), false },
        { new Header(1), false },
        { new Header(3), false },
        { new Header(Type:1, Length:2), false },
        { new Header(Type:0, Length:1), false },
        { new Header(Type:0, Length:0), false }
    };
    [Theory()]
    [MemberData(nameof(NAKTest))]
    public void Header_With_Length_of_2_and_Type_0_Is_NAK(Header header, bool expected)
    {
        //Arrange

        //Act

        //Assert
        Assert.Equal(expected, header.IsNAK);
    }
}
