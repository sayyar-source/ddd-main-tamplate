using System.ComponentModel.DataAnnotations;

namespace PrintBridge.Application.DTOs.Printer;

public class ConnectRequestDto
{
    [Required]
    public string Mode { get; set; } = string.Empty; // "usb" | "lan"

    public string? IpAddress { get; set; }
    public int? Port { get; set; }
}
