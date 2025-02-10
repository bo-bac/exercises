using SdrHost;
using SdrHost.ControlItems.ReceiverState;
using SdrHost.Messages;

namespace Tests.ControlItems;

public class ReceiverStateControlItemTest
{
    [Fact]
    public async Task StartReceiverControlItemRequest_Throws_If_Disconnected()
    {
        //Arrange
        var targetClient = new TargetClient();
        var message = new StartReceiverControlItemRequest();

        //Act
        var exception = await Assert.ThrowsAsync<ApplicationException>(() => targetClient.Send(message));

        //Assert
        Assert.False(targetClient.IsConnected);
        Assert.StartsWith("Client should be connected.", exception.Message);
    }

    public static TheoryData<StartReceiverControlItemRequest, ControlMessage, ControlMessage, StartParameter> StartTestData => new()
    {
        // 4.2.1 Receiver State
        // START
        {
            new StartReceiverControlItemRequest(new StartParameter(DataMode.Real, CaptureMode.Fifo, 1)),
            new ControlMessage(
                code: StartReceiverControlItemRequest.CODE,
                type: (byte)MessageTypes.HostToTarget.Set,
                parameter: [0x00,0x02,0x01,0x01]
            ),
            new ControlMessage(
                code: StartReceiverControlItemRequest.CODE,
                type: (byte)MessageTypes.TargetToHost.Response,
                parameter: [0x00,0x02,0x01,0x01]
            ),
            new StartParameter(DataMode.Real, CaptureMode.Fifo, 1)
        },
        {
            new StartReceiverControlItemRequest(),
            new ControlMessage(
                code: StartReceiverControlItemRequest.CODE,
                type: (byte)MessageTypes.HostToTarget.Set,
                parameter: [0x00,0x02,0x01,0x01]
            ),
            new ControlMessage(
                code: StartReceiverControlItemRequest.CODE,
                type: (byte)MessageTypes.TargetToHost.Response,
                parameter: [0x00,0x02,0x01,0x01]
            ),
            new StartParameter(DataMode.Real, CaptureMode.Fifo, 1)
        },
    };

    [Theory]
    [MemberData(nameof(StartTestData))]
    public void StartReceiverControlItemRequest_Should_Be_Relpayed_By_ReceiverStateControlItemResponse(
        StartReceiverControlItemRequest request,
        ControlMessage requestMessage,
        ControlMessage responseMessage,
        StartParameter expectedResponse
        )
    {
        //Arrange

        //Act
        var response = new ReceiverStateControlItemResponse(responseMessage);

        //Assert Item -> Message
        Assert.Equal(requestMessage.Type, request.Type);
        Assert.Equal(requestMessage.Code, request.Code);
        Assert.Equal(requestMessage.Parameter, request.Parameter);

        //Assert Message -> Item
        Assert.False(response.IsNAK);
        Assert.Equal(DataCapturingControl.Run, response.DataCapturingControl);
        Assert.Equal(expectedResponse.CaptureMode, response.CaptureMode);
        Assert.Equal(expectedResponse.DataMode, response.DataMode);
        Assert.Equal(expectedResponse.N, response.N);

    }

    public static TheoryData<StopReceiverControlItemRequest, ControlMessage, ControlMessage> StopTestData => new()
    {
        // 4.2.1 Receiver State
        // STOP
        {
            new StopReceiverControlItemRequest(),
            new ControlMessage(
                code: ReceiverStateControlItemResponse.CODE,
                type: (byte)MessageTypes.TargetToHost.Response,
                parameter: [0x00,0x01,0x00,0x00]
            ),
            new ControlMessage(
                code: ReceiverStateControlItemResponse.CODE,
                type: (byte)MessageTypes.TargetToHost.Response,
                parameter: [0x00,0x01,0x00,0x00]
            )
        }
    };

    [Theory]
    [MemberData(nameof(StopTestData))]
    public void StopReceiverControlItemRequest_Should_Be_Relpayed_By_ReceiverStateControlItemResponse(
        StopReceiverControlItemRequest request,
        ControlMessage requestMessage,
        ControlMessage responseMessage
        )
    {
        //Arrange

        //Act
        var response = new ReceiverStateControlItemResponse(responseMessage);

        //Assert Item -> Message
        Assert.Equal(requestMessage.Type, request.Type);
        Assert.Equal(requestMessage.Code, request.Code);
        Assert.Equal(requestMessage.Parameter, request.Parameter);

        //Assert Message -> Item
        Assert.False(response.IsNAK);
        Assert.Equal(DataCapturingControl.Idle, response.DataCapturingControl);
    }
}