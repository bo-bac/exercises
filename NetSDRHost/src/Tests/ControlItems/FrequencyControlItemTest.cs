using System;
using SdrHost.ControlItems.ReceiverFrequency;
using SdrHost.Messages;

namespace Tests.ControlItems;

public class FrequencyControlItemTest
{
    public static TheoryData<GetFrequencyControlItemRequest, ControlMessage, ControlMessage, FrequencyChannel, ulong> GetTestData => new()
    {
        // 4.2.3 Receiver Frequency
        // get
        {
            new GetFrequencyControlItemRequest(FrequencyChannel.Channel2),
            new ControlMessage(
                code: GetFrequencyControlItemRequest.CODE,
                type: (byte)MessageTypes.HostToTarget.Get,
                parameter: [0x02]
            ),
            new ControlMessage(
                code: SetFrequencyControlItemRequest.CODE,
                type: (byte)MessageTypes.TargetToHost.Response,
                parameter: [0x02, 0x90,0xC6,0xD5,0x00,0x00]
            ),
            FrequencyChannel.Channel2,
            14010000
        }
    };

    [Theory]
    [MemberData(nameof(GetTestData))]
    public void GetFrequencyControlItemRequest_Should_Be_Relpayed_By_FrequencyControlItemResponse(
        GetFrequencyControlItemRequest request,
        ControlMessage requestMessage,
        ControlMessage responseMessage,
        FrequencyChannel expChannel, 
        ulong expFrequency
        )
    {
        //Arrange

        //Act
        var response = new FrequencyControlItemResponse(responseMessage);

        //Assert Item -> Message
        Assert.Equal(requestMessage.Type, request.Type);
        Assert.Equal(requestMessage.Code, request.Code);
        Assert.Equal(requestMessage.Parameter, request.Parameter);

        //Assert Message -> Item
        Assert.False(response.IsNAK);
        Assert.Equal(expChannel, response.Channel);
        Assert.Equal(expFrequency, response.Frequency);
    }

    public static TheoryData<SetFrequencyControlItemRequest, ControlMessage, ControlMessage, SetFrequencyParameter> SetTestData => new()
    {
        // 4.2.3 Receiver Frequency
        // set
        {
            new SetFrequencyControlItemRequest(new(FrequencyChannel.AllChannels, 14010000)),
            new ControlMessage(
                code: SetFrequencyControlItemRequest.CODE,
                type: (byte)MessageTypes.HostToTarget.Set,
                parameter: [0xFF, 0x90,0xC6,0xD5,0x00,0x00]
            ),
            new ControlMessage(
                code: SetFrequencyControlItemRequest.CODE,
                type: (byte)MessageTypes.TargetToHost.Response,
                parameter: [0xFF, 0x90,0xC6,0xD5,0x00,0x00]
            ),
            new(FrequencyChannel.AllChannels, 14010000)
        }
    };

    [Theory]
    [MemberData(nameof(SetTestData))]
    public void SetFrequencyControlItemRequest_Should_Be_Relpayed_By_FrequencyControlItemResponse(
        SetFrequencyControlItemRequest request,
        ControlMessage requestMessage,
        ControlMessage responseMessage,
        SetFrequencyParameter expected
        )
    {
        //Arrange

        //Act
        var response = new FrequencyControlItemResponse(responseMessage);

        //Assert Item -> Message
        Assert.Equal(requestMessage.Type, request.Type);
        Assert.Equal(requestMessage.Code, request.Code);
        Assert.Equal(requestMessage.Parameter, request.Parameter);

        //Assert Message -> Item
        Assert.False(response.IsNAK);
        Assert.Equal(expected.Channel, response.Channel);
        Assert.Equal(expected.Frequency, response.Frequency);
    }
}
