using SdrHost.ControlItems.ReceiverFrequency;
using SdrHost.ControlItems.ReceiverState;
using SdrHost.ControlItems.TargetName;
using SdrHost.Messages;

namespace SdrHost.Controllers;

internal class ResponseController(
    TargetClient client,
    Action<string> stdout
)
{
    public Func<ControlMessage, Task> this[ushort code]
    {
        get => code switch
        {
            //4.2.1 Receiver State
            0x0018 => async (message) =>
            {
                var response = new ReceiverStateControlItemResponse(message);
                client.IsCapturing = response.IsCapturingData;

                if (response.IsCapturingData)
                {
                    stdout($"[{client.Label}] Started: {response.DataMode} {response.CaptureMode} {response.N}");
                }
                else
                {
                    stdout($"[{client.Label}] Stopped.");
                }
            }
            ,

            //4.2.3 Receiver Frequency
            0x0020 => async (message) =>
            {
                var response = new FrequencyControlItemResponse(message);
                client.Frequency = response.Frequency;
                stdout($"[{client.Label}] {response.Channel} Frequency: {response.Frequency}");
            }
            ,

            //4.1.1 Target Name
            0x0001 => async (message) =>
            {
                var response = new TargetNameControlItemResponse(message);
                stdout($"[{client.Label}] Target Name: {response.Name}");
            }
            ,

            _ => async (message) => stdout("Unknown incomming message.")
        };
    }
}
