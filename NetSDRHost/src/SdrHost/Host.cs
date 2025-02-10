using SdrHost.Controllers;
using SdrHost.Messages;

namespace SdrHost;

public sealed class Host : IDisposable
{
    private Action<string> _console = (_) => { };
    private TargetClient _client;
    private CommandController _commands;
    private ResponseController _responseHandlers;

    private FileStream _fileStream;

    public async Task RunAsync(Func<string?> input, Action<string> console, string dataFileName)
    {
        _client = new();
        _client.ResponceMessageReceived += HandlResponceMessageReceivedasync;
        _client.UnsolicitedMessageReceived += HandlUnsolicitedMessageReceivedAsync;
        _client.DataMessageReceived += HandleDataMessageReceivedAsync;

        _commands = new CommandController(_client, console);
        _responseHandlers = new ResponseController(_client, console);

        _fileStream = new FileStream(dataFileName, FileMode.Create, FileAccess.Write, FileShare.None);

        _console = console;
        _console("SDR Host v0.1");

        await _commands["connect"]([]);
        await Task.Run(() => HandleUserInput(input));
    }

    private async Task HandleUserInput(Func<string?> stdin)
    {
        while (true)
        {
            _console("Enter command: start, stop, freq [value]");

            var input = stdin()?.Trim();

            var parts = input?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = parts?[0];
            var argss = parts?.Length > 1 ? parts[1..] : [];

            try
            {
                await _commands[command](argss);
            }
            catch (Exception ex)
            {
                _console(ex.Message);
            }
        }
    }

    private async Task HandlResponceMessageReceivedasync(ControlMessage message)
    {
        try
        {
            await _responseHandlers[message.Code](message);
        }
        catch (Exception ex)
        {
            _console(ex.Message);
        }
    }

    private async Task HandlUnsolicitedMessageReceivedAsync(ControlMessage message)
    {
        try
        {
            _console("Unsolicited detected.");
            await _responseHandlers[message.Code](message);
        }
        catch (Exception ex)
        {
            _console(ex.Message);
        }
    }

    private async Task HandleDataMessageReceivedAsync(Real16FifoDataMessage message)
    {
        try
        {
            if (message.Data != null)
            {
                await _fileStream.WriteAsync(message.Data.AsMemory(0, message.Data.Length));
                await _fileStream.FlushAsync();
            }
        }
        catch (Exception ex)
        {
            _console(ex.Message);
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
        _fileStream?.Dispose();
    }
}
