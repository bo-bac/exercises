using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SdrHost.Messages;

namespace SdrHost;

public interface ITargetClient
{
    bool IsConnected { get; }
    bool IsReadyToDataCapture(out string? message);
    bool IsCapturing { get; }

    Task Send(ControlMessage request);
}

internal class TargetClient : ITargetClient, IDisposable
{
    private TcpClient? _tcpClient;
    private NetworkStream? _tcpStream;
    private CancellationTokenSource? _tcpCts;

    private readonly ConcurrentDictionary<ushort, bool> _nakedCodes = new();

    private UdpClient? _udpClient;
    private CancellationTokenSource? _udpCts;

    public event Func<ControlMessage, Task>? UnsolicitedMessageReceived;
    public event Func<ControlMessage, Task>? ResponceMessageReceived;
    public event Func<Real16FifoDataMessage, Task>? DataMessageReceived;

    public IPAddress Ip { get; init; } = IPAddress.Parse("127.0.0.1");
    public int ControlPort { get; init; } = 50000; //tcp
    public int DataPort { get; init; } = 60000; //udp
    public int ControlResponseTimeout { get; set; } = 5000; //5 sec
    public string Label => $"{Ip}:{ControlPort}";

    public ulong Frequency { get; set; }

    private bool _isCapturing;
    public bool IsCapturing
    {
        get => _isCapturing;
        set
        {
            //if (_isCapturing == value) return;

            if (value)
            {
                StartUdp();
            }
            else
            {
                StopUdp();
            }
            _isCapturing = value;
        }
    }

    public bool IsConnected => _tcpClient?.Connected ?? false;
    public bool IsReadyToDataCapture(out string? message)
    {
        var sb = new StringBuilder();

        if (!IsConnected)
        {
            sb.AppendLine("Client should be connected.");
        }

        if (Frequency == 0)
        {
            sb.AppendLine("Frequency should not be 0.");
        }

        message = sb.Length > 0 ? sb.ToString() : null;

        return message is null;
    }

    public void Connect()
    {
        StartTcp();
    }

    public void Disconnect()
    {
        StopTcp();
        _nakedCodes.Clear();
    }

    public async Task Send(ControlMessage request)
    {
        if (!IsConnected)
        {
            throw new ApplicationException("Client should be connected.");
        }

        // write request
        byte[] dataToSend = request.Serialize();
        await _tcpStream!.WriteAsync(dataToSend);
    }

    public void Dispose()
    {
        StopTcp();
        _tcpCts?.Dispose();
        _tcpCts = null;
        _tcpClient?.Dispose();
        _tcpClient = null;

        StopUdp();
        _udpCts?.Dispose();
        _udpCts = null;
        _udpClient?.Dispose();
        _udpClient = null;
    }

    private void StartTcp()
    {
        if (_tcpClient is not null && _tcpClient.Connected) return;

        _tcpClient = new TcpClient();
        _tcpClient.Connect(Ip, ControlPort);
        _tcpStream = _tcpClient.GetStream();
        _tcpCts = new CancellationTokenSource();

        var stream = _tcpClient.GetStream();
        _ = Task.Run(async () =>
        {
            while (!_tcpCts.Token.IsCancellationRequested)
            {
                try
                {
                    if (!IsConnected) break;
                    var message = await ControlMessage.Deserialize(stream, _tcpCts.Token);
                    if (message is null) continue;

                    await HandleTargetResponse(message);
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("[TCP STOPPED] Control communication canceled cleanly.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TCP BROKEN] {ex.Message}");
                }
            }

        }, _tcpCts.Token);
    }

    private void StopTcp()
    {
        _tcpCts?.Cancel();
        _tcpClient?.Close();
    }

    private async Task HandleTargetResponse(ControlMessage message)
    {
        switch (message.GetHeader().Type)
        {
            case (byte)MessageTypes.TargetToHost.Response:
                await (ResponceMessageReceived?.Invoke(message) ?? Task.CompletedTask);
                break;

            case (byte)MessageTypes.TargetToHost.Unsolicited:
                await (UnsolicitedMessageReceived?.Invoke(message) ?? Task.CompletedTask);
                break;

            default:
                // here is unknown Message Types from Target
                // (some bullshit or whatever)
                // handel or skip them
                // !!! but DON'T throw

                //var buffer = message.Serialize();
                //throw new ApplicationException($"Unhandled incomming: {BitConverter.ToString(buffer)}");
                break;
        }
    }

    private void StartUdp()
    {
        if (_udpClient?.Client != null) return;

        // not sure who-is-who in this communication
        // so assume we are client
        _udpClient = new UdpClient();
        _udpClient.Connect(Ip, DataPort);
        _udpCts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            await _udpClient.SendAsync([], 0);

            while (!_udpCts.Token.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync(_udpCts.Token);
                    var message = Real16FifoDataMessage.Deserialize(result.Buffer);
                    if (message != null && DataMessageReceived != null)
                    {
                        await DataMessageReceived.Invoke(message);
                    }
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("[UDP STOPPED] Data capturing canceled cleanly.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[UDP BROKEN] {ex.Message}");
                }
            }

        }, _udpCts.Token);
    }

    private void StopUdp()
    {
        _udpCts?.Cancel();
        _udpClient?.Close();
    }
}
