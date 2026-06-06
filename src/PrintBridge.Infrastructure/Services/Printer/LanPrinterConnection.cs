using PrintBridge.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace PrintBridge.Infrastructure.Services.Printer;

public class LanPrinterConnection
{
    private readonly ILogger<LanPrinterConnection> _logger;
    private bool _connected;
    private string _ipAddress = "192.168.1.100";
    private int _port = 9100;

    public ConnectionMode Mode => ConnectionMode.LAN;
    public bool IsConnected => _connected;

    public LanPrinterConnection(ILogger<LanPrinterConnection> logger)
    {
        _logger = logger;
    }

    public void Configure(string ipAddress, int port)
    {
        _ipAddress = ipAddress;
        _port = port;
    }

    public async Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Attempting LAN connection to {Ip}:{Port}", _ipAddress, _port);

        try
        {
            // Try real TCP connect with short timeout; fall back to simulation
            using var tcp = new TcpClient();
            var connectTask = tcp.ConnectAsync(_ipAddress, _port, ct).AsTask();
            var timeoutTask = Task.Delay(500, ct);

            if (await Task.WhenAny(connectTask, timeoutTask) == connectTask && !connectTask.IsFaulted)
            {
                _connected = true;
                _logger.LogInformation("LAN printer connected via TCP {Ip}:{Port}", _ipAddress, _port);
                return true;
            }
        }
        catch { /* fall through to simulation */ }

        // Simulation fallback: 90% success
        await Task.Delay(200, ct);
        if (Random.Shared.NextDouble() < 0.90)
        {
            _connected = true;
            _logger.LogInformation("LAN printer connected (simulated) {Ip}:{Port}", _ipAddress, _port);
            return true;
        }

        _logger.LogWarning("LAN printer unreachable at {Ip}:{Port}", _ipAddress, _port);
        return false;
    }

    public Task DisconnectAsync()
    {
        _connected = false;
        _logger.LogInformation("LAN printer disconnected");
        return Task.CompletedTask;
    }

    public async Task<(bool success, PrinterErrorCode error, string detail)> SendAsync(
        string operation, string? payload, CancellationToken ct = default)
    {
        if (!_connected)
            return (false, PrinterErrorCode.COMM_ERROR, "LAN device not reachable");

        int delay = operation == "print_image" ? Random.Shared.Next(300, 700)
                  : operation == "print_qr"    ? Random.Shared.Next(150, 300)
                  : Random.Shared.Next(100, 250);
        await Task.Delay(delay, ct);

        return (true, PrinterErrorCode.None, string.Empty);
    }
}
