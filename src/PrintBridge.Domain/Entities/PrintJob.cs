using PrintBridge.Domain.Enums;

namespace PrintBridge.Domain.Entities;

public class PrintJob
{
    public string JobId { get; set; } = GenerateId();
    public string Operation { get; set; } = string.Empty;
    public ConnectionMode Connection { get; set; }
    public PrintJobStatus Status { get; set; } = PrintJobStatus.Queued;
    public string? Content { get; set; }
    public string ContentType { get; set; } = "text";
    public string Language { get; set; } = "tr";
    public PrinterErrorCode ErrorCode { get; set; } = PrinterErrorCode.None;
    public string? ErrorDetail { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int RetryCount { get; set; }
    public string? IdempotencyKey { get; set; }

    private static string GenerateId() =>
        Guid.NewGuid().ToString("N")[..8].ToUpper();
}
