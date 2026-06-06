using System.Net.Http.Json;
using System.Text.Json;
using PrintBridge.Blazor.DTO.Printer;

namespace PrintBridge.Blazor.Services;

public class PrinterApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PrinterApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("ApiClient");
    }

    public async Task<(bool ok, string message)> ConnectAsync(ConnectRequest req)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/printer/connect", req);
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<MessageResponse>(_json);
                return (true, body?.Message ?? "Connected");
            }
            var err = await res.Content.ReadFromJsonAsync<ErrorResponse>(_json);
            return (false, err?.Error ?? "Connection failed");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<PrinterStatusResponse?> GetStatusAsync()
    {
        try { return await _http.GetFromJsonAsync<PrinterStatusResponse>("api/printer/status", _json); }
        catch { return null; }
    }

    public async Task<JobListResponse?> GetJobsAsync(string? status = null, int page = 1, int pageSize = 20)
    {
        try
        {
            var qs = $"api/printer/jobs?page={page}&pageSize={pageSize}";
            if (status != null) qs += $"&status={status}";
            return await _http.GetFromJsonAsync<JobListResponse>(qs, _json);
        }
        catch { return null; }
    }

    public async Task<(bool ok, PrintJobResponse? job, string error)> PrintTextAsync(PrintTextRequest req)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/printer/print/text", req);
            if (res.IsSuccessStatusCode)
            {
                var job = await res.Content.ReadFromJsonAsync<PrintJobResponse>(_json);
                return (true, job, string.Empty);
            }
            var err = await res.Content.ReadFromJsonAsync<ErrorResponse>(_json);
            return (false, null, err?.Error ?? "Print failed");
        }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool ok, PrintJobResponse? job, string error)> PrintImageAsync(PrintImageRequest req)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/printer/print/image", req);
            if (res.IsSuccessStatusCode)
            {
                var job = await res.Content.ReadFromJsonAsync<PrintJobResponse>(_json);
                return (true, job, string.Empty);
            }
            var err = await res.Content.ReadFromJsonAsync<ErrorResponse>(_json);
            return (false, null, err?.Error ?? "Print failed");
        }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool ok, PrintJobResponse? job, string error)> PrintQrAsync(PrintQrRequest req)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/printer/print/qr", req);
            if (res.IsSuccessStatusCode)
            {
                var job = await res.Content.ReadFromJsonAsync<PrintJobResponse>(_json);
                return (true, job, string.Empty);
            }
            var err = await res.Content.ReadFromJsonAsync<ErrorResponse>(_json);
            return (false, null, err?.Error ?? "QR print failed");
        }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool ok, PrintJobResponse? job, string error)> ReprintAsync(string jobId)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/printer/reprint", new ReprintRequest { JobId = jobId });
            if (res.IsSuccessStatusCode)
            {
                var job = await res.Content.ReadFromJsonAsync<PrintJobResponse>(_json);
                return (true, job, string.Empty);
            }
            var err = await res.Content.ReadFromJsonAsync<ErrorResponse>(_json);
            return (false, null, err?.Error ?? "Reprint failed");
        }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task SimulateErrorAsync(string code)
    {
        try { await _http.PostAsync($"api/printer/simulate/error?code={code}", null); }
        catch { /* ignore */ }
    }

    public async Task ClearErrorAsync(string code)
    {
        try { await _http.PostAsync($"api/printer/simulate/clear?code={code}", null); }
        catch { /* ignore */ }
    }

    private record MessageResponse(string Message);
    private record ErrorResponse(string Error);
}
