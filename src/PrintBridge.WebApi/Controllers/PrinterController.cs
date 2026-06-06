using Microsoft.AspNetCore.Mvc;
using PrintBridge.Application.DTOs.Printer;
using PrintBridge.Domain.Enums;
using PrintBridge.Infrastructure.Services.Printer;
using System.Text;

namespace PrintBridge.WebApi.Controllers;

[ApiController]
[Route("api/printer")]
public class PrinterController : ControllerBase
{
    private readonly PrinterManager _printer;
    private readonly PrintLogRepository _log;

    public PrinterController(PrinterManager printer, PrintLogRepository log)
    {
        _printer = printer;
        _log = log;
    }

    // POST /api/printer/connect
    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectRequestDto dto)
    {
        var (ok, message) = await _printer.ConnectAsync(dto.Mode, dto.IpAddress, dto.Port);
        return ok ? Ok(new { message }) : BadRequest(new { error = message });
    }

    // POST /api/printer/print/text
    [HttpPost("print/text")]
    public async Task<IActionResult> PrintText([FromBody] PrintTextRequestDto dto)
    {
        if (!_printer.IsConnected)
            return Conflict(new { error = "Printer not connected. Call /connect first." });

        var job = await _printer.EnqueuePrintTextAsync(dto);
        return Accepted(MapJob(job));
    }

    // POST /api/printer/print/image
    [HttpPost("print/image")]
    public async Task<IActionResult> PrintImage([FromBody] PrintImageRequestDto dto)
    {
        if (!_printer.IsConnected)
            return Conflict(new { error = "Printer not connected. Call /connect first." });

        var job = await _printer.EnqueuePrintImageAsync(dto);
        return Accepted(MapJob(job));
    }

    // POST /api/printer/print/qr  (bonus)
    [HttpPost("print/qr")]
    public async Task<IActionResult> PrintQr([FromBody] PrintQrRequestDto dto)
    {
        if (!_printer.IsConnected)
            return Conflict(new { error = "Printer not connected. Call /connect first." });

        var job = await _printer.EnqueuePrintQrAsync(dto);
        return Accepted(MapJob(job));
    }

    // GET /api/printer/status
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(_printer.GetStatus());
    }

    // GET /api/printer/jobs
    [HttpGet("jobs")]
    public IActionResult GetJobs(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var jobs = _printer.GetJobs();
        if (status != null)
            jobs = jobs.Where(j => j.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();

        var paged = jobs
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapJob);

        return Ok(new { total = jobs.Count, page, pageSize, items = paged });
    }

    // GET /api/printer/jobs/{id}
    [HttpGet("jobs/{jobId}")]
    public IActionResult GetJob(string jobId)
    {
        var job = _printer.GetJob(jobId);
        return job == null ? NotFound() : Ok(MapJob(job));
    }

    // GET /api/printer/logs
    [HttpGet("logs")]
    public IActionResult GetLogs()
    {
        return Ok(_log.GetAll());
    }

    // GET /api/printer/logs/export  (bonus: CSV download)
    [HttpGet("logs/export")]
    public IActionResult ExportLogs([FromQuery] string format = "csv")
    {
        var csv = _log.ExportCsv();
        var bytes = Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", $"printer-logs-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    // POST /api/printer/reprint
    [HttpPost("reprint")]
    public async Task<IActionResult> Reprint([FromBody] ReprintRequestDto dto)
    {
        var (ok, job, error) = await _printer.ReprintAsync(dto);
        if (!ok) return BadRequest(new { error });
        return Accepted(MapJob(job!));
    }

    // POST /api/printer/simulate/error  (testing helper)
    [HttpPost("simulate/error")]
    public IActionResult SimulateError([FromQuery] string code)
    {
        _printer.SimulateError(code);
        return Ok(new { message = $"Simulated error '{code}' injected" });
    }

    // POST /api/printer/simulate/clear  (testing helper)
    [HttpPost("simulate/clear")]
    public IActionResult ClearError([FromQuery] string code)
    {
        _printer.ClearError(code);
        return Ok(new { message = $"Error '{code}' cleared" });
    }

    private static object MapJob(PrintBridge.Domain.Entities.PrintJob j) => new
    {
        jobId = j.JobId,
        operation = j.Operation,
        connection = j.Connection.ToString().ToLower(),
        status = j.Status.ToString().ToLower(),
        contentType = j.ContentType,
        errorCode = j.ErrorCode == PrinterErrorCode.None ? null : j.ErrorCode.ToString(),
        errorDetail = j.ErrorDetail,
        createdAt = j.CreatedAt,
        completedAt = j.CompletedAt,
        retryCount = j.RetryCount
    };
}
