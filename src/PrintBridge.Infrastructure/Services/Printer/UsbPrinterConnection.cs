using PrintBridge.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace PrintBridge.Infrastructure.Services.Printer;

public class UsbPrinterConnection
{
    private readonly ILogger<UsbPrinterConnection> _logger;
    private bool _connected;

    public ConnectionMode Mode => ConnectionMode.USB;
    public bool IsConnected => _connected;

    public UsbPrinterConnection(ILogger<UsbPrinterConnection> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        await Task.Delay(150, ct);

        // Simulate USB enumeration: ~95% success
        if (Random.Shared.NextDouble() < 0.95)
        {
            _connected = true;
            _logger.LogInformation("USB printer connected");
            return true;
        }

        _logger.LogWarning("USB printer not detected");
        return false;
    }

    public Task DisconnectAsync()
    {
        _connected = false;
        _logger.LogInformation("USB printer disconnected");
        return Task.CompletedTask;
    }

    public async Task<(bool success, PrinterErrorCode error, string detail)> SendAsync(
        string operation, string? payload, CancellationToken ct = default)
    {
        if (!_connected)
            return (false, PrinterErrorCode.COMM_ERROR, "USB device not connected");

        // Simulate print latency
        int delay = operation == "print_image" ? Random.Shared.Next(400, 900)
                  : operation == "print_qr"    ? Random.Shared.Next(200, 400)
                  : Random.Shared.Next(150, 350);
        await Task.Delay(delay, ct);

        return (true, PrinterErrorCode.None, string.Empty);
    }
}
