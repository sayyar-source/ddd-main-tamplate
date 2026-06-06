namespace PrintBridge.Application.DTOs.Printer;

public class PrintJobDto
{
    public string JobId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Connection { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public string? ErrorDetail { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int RetryCount { get; set; }
}
