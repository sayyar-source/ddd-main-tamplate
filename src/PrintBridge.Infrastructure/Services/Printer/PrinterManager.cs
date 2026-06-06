using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PrintBridge.Application.DTOs.Printer;
using PrintBridge.Domain.Entities;
using PrintBridge.Domain.Enums;

namespace PrintBridge.Infrastructure.Services.Printer;

public class PrinterManager : BackgroundService
{
    private readonly ILogger<PrinterManager> _logger;
    private readonly PrintLogRepository _log;
    private readonly UsbPrinterConnection _usb;
    private readonly LanPrinterConnection _lan;

    // Channel for sequential job processing (at most 1 in-flight print)
    private readonly Channel<PrintJob> _channel = Channel.CreateUnbounded<PrintJob>(
        new UnboundedChannelOptions { SingleReader = true });

    private readonly ConcurrentDictionary<string, PrintJob> _jobs = new();
    private readonly ConcurrentDictionary<string, string> _idempotencyMap = new(); // key -> jobId

    // Manual queue depth counter — Channel.Reader.Count throws NotSupportedException with SingleReader=true
    private int _queueDepth;

    // Mutable printer state
    private ConnectionMode _targetMode = ConnectionMode.USB;
    private bool _connected;
    private float _paperPercent = 100f;
    private bool _coverOpen;
    private float _temperature = 34f;
    private string? _lastError;
    private int _reconnectAttempts;

    public bool IsConnected => _connected;
    public ConnectionMode ActiveMode => _targetMode;
    public string PaperStatus => _paperPercent <= 0 ? "out" : _paperPercent < 15 ? "low" : "ok";
    public string CoverStatus => _coverOpen ? "open" : "closed";
    public float Temperature => _temperature;
    public string? LastError => _lastError;
    public int QueueDepth => _queueDepth;
    public int FailedCount => _jobs.Values.Count(j => j.Status == PrintJobStatus.Failed);
    public PrintJob? LastCompletedJob => _jobs.Values
        .Where(j => j.Status is PrintJobStatus.Completed or PrintJobStatus.Failed)
        .OrderByDescending(j => j.CompletedAt)
        .FirstOrDefault();

    // Bonus: paper roll life estimate (~120 prints per roll)
    public int EstimatedPrintsRemaining => (int)(_paperPercent / 100f * 120);

    public PrinterManager(
        ILogger<PrinterManager> logger,
        PrintLogRepository log,
        UsbPrinterConnection usb,
        LanPrinterConnection lan)
    {
        _logger = logger;
        _log = log;
        _usb = usb;
        _lan = lan;
    }

    // ── Connect ──────────────────────────────────────────────────────────────

    public async Task<(bool ok, string message)> ConnectAsync(
        string mode, string? ip = null, int? port = null, CancellationToken ct = default)
    {
        if (!Enum.TryParse<ConnectionMode>(mode, ignoreCase: true, out var connMode))
            return (false, $"Unknown mode '{mode}'. Use 'usb' or 'lan'.");

        _targetMode = connMode;

        if (connMode == ConnectionMode.LAN && ip != null)
            _lan.Configure(ip, port ?? 9100);

        _connected = false;
        _reconnectAttempts = 0;
        bool ok = await DoConnectAsync(ct);
        return ok
            ? (true, $"Connected via {connMode}")
            : (false, $"Failed to connect via {connMode}");
    }

    private async Task<bool> DoConnectAsync(CancellationToken ct = default)
    {
        bool ok = _targetMode == ConnectionMode.USB
            ? await _usb.ConnectAsync(ct)
            : await _lan.ConnectAsync(ct);

        if (ok)
        {
            _connected = true;
            _lastError = null;
            _reconnectAttempts = 0;
        }
        else
        {
            _connected = false;
        }

        return ok;
    }

    // ── Enqueue ───────────────────────────────────────────────────────────────

    public async Task<PrintJob> EnqueuePrintTextAsync(PrintTextRequestDto req)
    {
        if (req.IdempotencyKey != null && _idempotencyMap.TryGetValue(req.IdempotencyKey, out var existing))
            return _jobs[existing];

        var job = new PrintJob
        {
            Operation = "print_text",
            Connection = _targetMode,
            Content = req.Text,
            ContentType = "text",
            Language = req.Language,
            IdempotencyKey = req.IdempotencyKey
        };
        return EnqueueJob(job);
    }

    public async Task<PrintJob> EnqueuePrintImageAsync(PrintImageRequestDto req)
    {
        if (req.IdempotencyKey != null && _idempotencyMap.TryGetValue(req.IdempotencyKey, out var existing))
            return _jobs[existing];

        var job = new PrintJob
        {
            Operation = "print_image",
            Connection = _targetMode,
            Content = req.ImageBase64,
            ContentType = "image",
            IdempotencyKey = req.IdempotencyKey
        };
        return EnqueueJob(job);
    }

    public async Task<PrintJob> EnqueuePrintQrAsync(PrintQrRequestDto req)
    {
        if (req.IdempotencyKey != null && _idempotencyMap.TryGetValue(req.IdempotencyKey, out var existing))
            return _jobs[existing];

        var job = new PrintJob
        {
            Operation = "print_qr",
            Connection = _targetMode,
            Content = req.Content,
            ContentType = "qr",
            IdempotencyKey = req.IdempotencyKey
        };
        return EnqueueJob(job);
    }

    public async Task<(bool ok, PrintJob? job, string error)> ReprintAsync(ReprintRequestDto req)
    {
        if (!_jobs.TryGetValue(req.JobId, out var original))
            return (false, null, "Job not found");

        if (original.Status == PrintJobStatus.Completed)
            return (false, null, "Job already completed successfully");

        var retry = new PrintJob
        {
            Operation = original.Operation,
            Connection = _targetMode,
            Content = original.Content,
            ContentType = original.ContentType,
            Language = original.Language,
        };
        return (true, EnqueueJob(retry), string.Empty);
    }

    private PrintJob EnqueueJob(PrintJob job)
    {
        _jobs[job.JobId] = job;
        if (job.IdempotencyKey != null)
            _idempotencyMap[job.IdempotencyKey] = job.JobId;

        _channel.Writer.TryWrite(job);
        Interlocked.Increment(ref _queueDepth);
        _logger.LogInformation("Job {JobId} ({Op}) queued", job.JobId, job.Operation);
        return job;
    }

    // ── Status & Query ────────────────────────────────────────────────────────

    public PrinterStatusDto GetStatus()
    {
        var last = LastCompletedJob;
        return new PrinterStatusDto
        {
            IsConnected = _connected,
            ActiveMode = _targetMode.ToString().ToLower(),
            PaperStatus = PaperStatus,
            CoverStatus = CoverStatus,
            TemperatureCelsius = _temperature,
            QueueDepth = QueueDepth,
            PendingCount = _jobs.Values.Count(j =>
                j.Status is PrintJobStatus.Queued or PrintJobStatus.Processing or PrintJobStatus.Retrying),
            FailedCount = FailedCount,
            LastJob = last == null ? null : MapToDto(last),
            LastError = _lastError,
            PaperRemainingPercent = _paperPercent,
            EstimatedPrintsRemaining = EstimatedPrintsRemaining
        };
    }

    public IReadOnlyList<PrintJob> GetJobs() => _jobs.Values.OrderByDescending(j => j.CreatedAt).ToList();
    public PrintJob? GetJob(string jobId) => _jobs.GetValueOrDefault(jobId);

    // ── Background Processing ─────────────────────────────────────────────────

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = MonitorConnectionAsync(stoppingToken);

        await foreach (var job in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessJobAsync(job, stoppingToken);
        }
    }

    private async Task ProcessJobAsync(PrintJob job, CancellationToken ct)
    {
        Interlocked.Decrement(ref _queueDepth);

        if (!_connected)
        {
            // Attempt reconnect before giving up
            bool reconnected = await RetryConnectAsync(ct);
            if (!reconnected)
            {
                FailJob(job, PrinterErrorCode.COMM_ERROR, "Printer not connected");
                await _log.AppendAsync(job);
                return;
            }
        }

        // Check hardware errors before printing
        var hwError = CheckHardwareErrors();
        if (hwError != PrinterErrorCode.None)
        {
            FailJob(job, hwError, DescribeError(hwError));
            await _log.AppendAsync(job);
            return;
        }

        job.Status = PrintJobStatus.Processing;
        _logger.LogInformation("Processing job {JobId} ({Op})", job.JobId, job.Operation);

        var (success, errorCode, detail) = _targetMode == ConnectionMode.USB
            ? await _usb.SendAsync(job.Operation, job.Content, ct)
            : await _lan.SendAsync(job.Operation, job.Content, ct);

        if (success)
        {
            job.Status = PrintJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            UpdateHardwareAfterPrint();
            _logger.LogInformation("Job {JobId} completed", job.JobId);
        }
        else
        {
            // Retry up to 3 times with exponential backoff
            if (job.RetryCount < 3)
            {
                job.RetryCount++;
                job.Status = PrintJobStatus.Retrying;
                int backoff = (int)Math.Pow(2, job.RetryCount) * 500;
                _logger.LogWarning("Job {JobId} retrying ({Attempt}/3) in {Delay}ms", job.JobId, job.RetryCount, backoff);
                await Task.Delay(backoff, ct);
                _channel.Writer.TryWrite(job);
                Interlocked.Increment(ref _queueDepth);
                return;
            }

            FailJob(job, errorCode, detail);
        }

        await _log.AppendAsync(job);
    }

    private async Task<bool> RetryConnectAsync(CancellationToken ct)
    {
        const int maxAttempts = 5;
        while (_reconnectAttempts < maxAttempts && !ct.IsCancellationRequested)
        {
            _reconnectAttempts++;
            int delay = (int)Math.Pow(2, _reconnectAttempts) * 200; // 400, 800, 1600, 3200, 6400
            _logger.LogWarning("Reconnect attempt {Attempt}/{Max} in {Delay}ms", _reconnectAttempts, maxAttempts, delay);
            await Task.Delay(delay, ct);

            if (await DoConnectAsync(ct))
                return true;
        }
        return false;
    }

    private async Task MonitorConnectionAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(5000, ct);

            // Simulate random disconnects (1% chance per check) to demonstrate reconnect
            if (_connected && Random.Shared.NextDouble() < 0.01)
            {
                _connected = false;
                _lastError = "Connection lost (simulated)";
                _logger.LogWarning("Printer disconnected unexpectedly (simulated)");
                await RetryConnectAsync(ct);
            }

            // Simulate temperature fluctuation
            _temperature = Math.Clamp(_temperature + (float)(Random.Shared.NextDouble() * 2 - 1), 30f, 75f);
        }
    }

    private PrinterErrorCode CheckHardwareErrors()
    {
        if (_paperPercent <= 0) return PrinterErrorCode.PAPER_OUT;
        if (_coverOpen) return PrinterErrorCode.COVER_OPEN;
        if (_temperature > 70f) return PrinterErrorCode.OVERHEAT;
        return PrinterErrorCode.None;
    }

    private void UpdateHardwareAfterPrint()
    {
        _paperPercent = Math.Max(0, _paperPercent - 0.8f);
        _temperature = Math.Min(75f, _temperature + 0.5f);
    }

    private void FailJob(PrintJob job, PrinterErrorCode error, string detail)
    {
        job.Status = PrintJobStatus.Failed;
        job.ErrorCode = error;
        job.ErrorDetail = detail;
        job.CompletedAt = DateTime.UtcNow;
        _lastError = $"{error}: {detail}";
        _logger.LogError("Job {JobId} failed: {Error} - {Detail}", job.JobId, error, detail);
    }

    private static string DescribeError(PrinterErrorCode code) => code switch
    {
        PrinterErrorCode.PAPER_OUT => "No paper detected",
        PrinterErrorCode.PAPER_JAM => "Paper jam detected",
        PrinterErrorCode.COVER_OPEN => "Printer cover is open",
        PrinterErrorCode.OVERHEAT => "Printer overheated — cooling down",
        PrinterErrorCode.COMM_ERROR => "Communication error",
        PrinterErrorCode.UNKNOWN_COMMAND => "Unknown or unsupported command",
        _ => "Unknown error"
    };

    // ── Test/Simulation Helpers ───────────────────────────────────────────────

    public void SimulateError(string errorCode)
    {
        if (Enum.TryParse<PrinterErrorCode>(errorCode, ignoreCase: true, out var code))
        {
            switch (code)
            {
                case PrinterErrorCode.PAPER_OUT: _paperPercent = 0; break;
                case PrinterErrorCode.COVER_OPEN: _coverOpen = true; break;
                case PrinterErrorCode.OVERHEAT: _temperature = 72f; break;
                default: _lastError = DescribeError(code); break;
            }
        }
    }

    public void ClearError(string errorCode)
    {
        if (Enum.TryParse<PrinterErrorCode>(errorCode, ignoreCase: true, out var code))
        {
            switch (code)
            {
                case PrinterErrorCode.PAPER_OUT: _paperPercent = 100f; break;
                case PrinterErrorCode.COVER_OPEN: _coverOpen = false; break;
                case PrinterErrorCode.OVERHEAT: _temperature = 34f; break;
            }
        }
        _lastError = null;
    }

    private static PrintJobDto MapToDto(PrintJob j) => new()
    {
        JobId = j.JobId,
        Operation = j.Operation,
        Connection = j.Connection.ToString().ToLower(),
        Status = j.Status.ToString().ToLower(),
        ContentType = j.ContentType,
        ErrorCode = j.ErrorCode == PrinterErrorCode.None ? null : j.ErrorCode.ToString(),
        ErrorDetail = j.ErrorDetail,
        CreatedAt = j.CreatedAt,
        CompletedAt = j.CompletedAt,
        RetryCount = j.RetryCount
    };
}
