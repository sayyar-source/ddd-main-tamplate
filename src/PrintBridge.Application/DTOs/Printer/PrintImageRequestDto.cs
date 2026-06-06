using System.ComponentModel.DataAnnotations;

namespace PrintBridge.Application.DTOs.Printer;

public class PrintImageRequestDto
{
    [Required]
    public string ImageBase64 { get; set; } = string.Empty;

    public string? IdempotencyKey { get; set; }
}

public class PrintQrRequestDto
{
    [Required]
    public string Content { get; set; } = string.Empty;

    public string? IdempotencyKey { get; set; }
}
