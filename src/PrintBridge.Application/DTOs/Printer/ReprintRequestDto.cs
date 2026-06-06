using System.ComponentModel.DataAnnotations;

namespace PrintBridge.Application.DTOs.Printer;

public class ReprintRequestDto
{
    [Required]
    public string JobId { get; set; } = string.Empty;
}
