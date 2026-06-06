namespace PrintBridge.Blazor.DTO.Printer;

public class PrinterStatusResponse
{
    public bool IsConnected { get; set; }
    public string? ActiveMode { get; set; }
    public string PaperStatus { get; set; } = "unknown";
    public string CoverStatus { get; set; } = "unknown";
    public float? TemperatureCelsius { get; set; }
    public int QueueDepth { get; set; }
    public int PendingCount { get; set; }
    public int FailedCount { get; set; }
    public PrintJobResponse? LastJob { get; set; }
    public string? LastError { get; set; }
    public float? PaperRemainingPercent { get; set; }
    public int? EstimatedPrintsRemaining { get; set; }
}

public class PrintJobResponse
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

public class JobListResponse
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<PrintJobResponse> Items { get; set; } = new();
}

public class ConnectRequest
{
    public string Mode { get; set; } = "usb";
    public string? IpAddress { get; set; }
    public int? Port { get; set; }
}

public class PrintTextRequest
{
    public string Text { get; set; } = string.Empty;
    public string Language { get; set; } = "tr";
    public string? IdempotencyKey { get; set; }
}

public class PrintImageRequest
{
    public string ImageBase64 { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
}

public class PrintQrRequest
{
    public string Content { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
}

public class ReprintRequest
{
    public string JobId { get; set; } = string.Empty;
}
