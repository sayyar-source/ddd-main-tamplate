using System.ComponentModel.DataAnnotations;

namespace PrintBridge.Application.DTOs.Printer;

public class PrintTextRequestDto
{
    [Required]
    public string Text { get; set; } = string.Empty;

    public string Language { get; set; } = "tr";
    public string? IdempotencyKey { get; set; }
}
