using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using PrintBridge.Domain.Entities;
using PrintBridge.Domain.Enums;

namespace PrintBridge.Infrastructure.Services.Printer;

public class PrintLogRepository
{
    private readonly string _logFilePath;
    private readonly ConcurrentQueue<PrintLogEntry> _entries = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PrintLogRepository(string logDirectory = "logs")
    {
        Directory.CreateDirectory(logDirectory);
        _logFilePath = Path.Combine(logDirectory, "printer-logs.json");
        LoadExisting();
    }

    private void LoadExisting()
    {
        if (!File.Exists(_logFilePath)) return;
        try
        {
            foreach (var line in File.ReadLines(_logFilePath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var entry = JsonSerializer.Deserialize<PrintLogEntry>(line, _jsonOpts);
                if (entry != null) _entries.Enqueue(entry);
            }
        }
        catch { /* corrupt log — start fresh */ }
    }

    public async Task AppendAsync(PrintJob job)
    {
        var entry = new PrintLogEntry
        {
            Ts = job.CompletedAt?.ToString("O") ?? DateTime.UtcNow.ToString("O"),
            Op = job.Operation,
            Conn = job.Connection.ToString().ToLower(),
            JobId = job.JobId,
            Status = job.Status == PrintJobStatus.Completed ? "success" : "error",
            Error = job.ErrorCode != PrinterErrorCode.None
                ? new PrintLogError { Code = job.ErrorCode.ToString(), Detail = job.ErrorDetail ?? "" }
                : null
        };

        _entries.Enqueue(entry);

        await _writeLock.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(_logFilePath,
                JsonSerializer.Serialize(entry, _jsonOpts) + Environment.NewLine);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public IReadOnlyList<PrintLogEntry> GetAll() => _entries.ToArray();

    public string ExportCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine("ts,op,conn,jobId,status,errorCode,errorDetail");
        foreach (var e in _entries)
        {
            sb.AppendLine(string.Join(',',
                CsvEscape(e.Ts), CsvEscape(e.Op), CsvEscape(e.Conn),
                CsvEscape(e.JobId), CsvEscape(e.Status),
                CsvEscape(e.Error?.Code ?? ""),
                CsvEscape(e.Error?.Detail ?? "")));
        }
        return sb.ToString();
    }

    private static string CsvEscape(string? value)
    {
        if (value == null) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return '"' + value.Replace("\"", "\"\"") + '"';
        return value;
    }
}

public class PrintLogEntry
{
    public string Ts { get; set; } = string.Empty;
    public string Op { get; set; } = string.Empty;
    public string Conn { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public PrintLogError? Error { get; set; }
}

public class PrintLogError
{
    public string Code { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}
