namespace PrintBridge.Application.DTOs.Printer;

public class PrinterStatusDto
{
    public bool IsConnected { get; set; }
    public string? ActiveMode { get; set; }
    public string PaperStatus { get; set; } = "unknown";
    public string CoverStatus { get; set; } = "unknown";
    public float? TemperatureCelsius { get; set; }
    public int QueueDepth { get; set; }
    public int PendingCount { get; set; }
    public int FailedCount { get; set; }
    public PrintJobDto? LastJob { get; set; }
    public string? LastError { get; set; }

    // Bonus: roll/receipt life estimate
    public float? PaperRemainingPercent { get; set; }
    public int? EstimatedPrintsRemaining { get; set; }
}
