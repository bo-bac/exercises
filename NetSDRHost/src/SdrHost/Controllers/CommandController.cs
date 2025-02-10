using SdrHost.ControlItems.ReceiverFrequency;
using SdrHost.ControlItems.ReceiverState;
using SdrHost.ControlItems.TargetName;
using SdrHost.Messages;

namespace SdrHost.Controllers;

internal class CommandController(
    TargetClient client,
    Action<string> stdout
    )
{
    public Func<string[], Task> this[string? command]
    {
        get => command switch
        {
            "connect" => async (args) =>
            {
                if (!client.IsConnected)
                {
                    client.Connect();
                }

                stdout($"[{client.Label}] Connected.");
            }
            ,

            "disconnect" => async (args) =>
            {
                if (client.IsConnected)
                {
                    client.Disconnect();
                }

                stdout($"[{client.Label}] Disconnected.");
            }
            ,

            "start" => async (args) =>
            {
                // validate
                if (!client.IsReadyToDataCapture(out var tip))
                {
                    throw new ApplicationException($"Is not ready to start: \n{tip}");
                }

                var message = new StartReceiverControlItemRequest();
                await client.Send(message);
                stdout("Target starting...");
            }
            ,

            "stop" => async (args) =>
            {
                var message = new StopReceiverControlItemRequest();
                await client.Send(message);
                stdout("Target stopping...");
            }
            ,

            "freq" => async (args) =>
            {
                // lets consider it works only in single channel mode (Channel1)
                ControlMessage message;
                if (args.Length == 0)
                {
                    // get
                    message = new GetFrequencyControlItemRequest(FrequencyChannel.Channel1);
                }
                else
                {
                    // set
                    var freq = ulong.Parse(args[0]);
                    message = new SetFrequencyControlItemRequest(new(FrequencyChannel.Channel1, freq));
                }

                await client.Send(message);
            }
            ,

            "name" => async (args) =>
            {
                var message = new GetTargetNameControlItemRequest();
                await client.Send(message);
            }
            ,

            "exit" => async (args) =>
            {
                Environment.Exit(0);
            }
            ,

            _ => async (args) => stdout("Unknown command.")
        };
    }
}
