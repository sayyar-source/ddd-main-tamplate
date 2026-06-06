# PrintBridge — Thermal Printer Service

A full-stack thermal printer management service built on ASP.NET Core 10 + Blazor Server.  
Supports USB and Ethernet (LAN) connections with auto-reconnect, print queue, error handling, structured logging, and a live dashboard UI.

---

## Architecture

```
PrintBridge/
├── src/
│   ├── PrintBridge.Domain/         # Entities, enums (PrintJob, ConnectionMode, …)
│   ├── PrintBridge.Application/    # DTOs (ConnectRequestDto, PrintJobDto, …)
│   ├── PrintBridge.Infrastructure/ # PrinterManager (BackgroundService), USB/LAN connections, PrintLogRepository
│   ├── PrintBridge.WebApi/         # REST API + /health
│   └── PrintBridge.Blazor/         # Live dashboard UI
├── Dockerfile
├── docker-compose.yml
└── logs/                           # printer-logs.json (auto-created)
```

**Key components:**
- `PrinterManager` — singleton `BackgroundService`; manages connection state, job queue (Channel<T>), retry/backoff, hardware simulation.
- `UsbPrinterConnection` / `LanPrinterConnection` — connection adapters; LAN tries real TCP, falls back to simulation.
- `PrintLogRepository` — appends JSONL logs to `logs/printer-logs.json`; exports CSV.

---

## Setup

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (local or via Docker)

### 1. Configure `.env` / appsettings

Copy `src/PrintBridge.WebApi/appsettings.json` and set:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=PrintBridgeDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Printer": {
    "LogDirectory": "logs"
  },
  "AllowedOrigins": "http://localhost:5172"
}
```

### 2. Run (single command)

```bash
# API (port 5160)
dotnet run --project src/PrintBridge.WebApi

# UI (port 5172, separate terminal)
dotnet run --project src/PrintBridge.Blazor
```

Swagger opens automatically at `http://localhost:5160/swagger`.  
Dashboard is at `http://localhost:5172/printer`.

### 3. Docker

```bash
docker-compose up --build
```

API: `http://localhost:5160` · Swagger: `http://localhost:5160/swagger`

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/printer/connect` | Connect USB or LAN |
| POST | `/api/printer/print/text` | Print text content |
| POST | `/api/printer/print/image` | Print base64 image |
| POST | `/api/printer/print/qr` | Print QR code |
| GET | `/api/printer/status` | Printer + queue status |
| GET | `/api/printer/jobs` | All jobs (paginated) |
| GET | `/api/printer/jobs/{id}` | Single job |
| GET | `/api/printer/logs` | JSON operation logs |
| GET | `/api/printer/logs/export` | Download CSV log |
| POST | `/api/printer/reprint` | Re-queue a failed job |
| GET | `/health` | Health check |
| POST | `/api/printer/simulate/error?code=X` | Inject test error |
| POST | `/api/printer/simulate/clear?code=X` | Clear test error |

---

## Test Examples (curl)

```bash
# Connect via USB
curl -X POST http://localhost:5160/api/printer/connect \
  -H "Content-Type: application/json" \
  -d '{"mode":"usb"}'

# Connect via LAN
curl -X POST http://localhost:5160/api/printer/connect \
  -H "Content-Type: application/json" \
  -d '{"mode":"lan","ipAddress":"192.168.1.100","port":9100}'

# Print text (Turkish receipt)
curl -X POST http://localhost:5160/api/printer/print/text \
  -H "Content-Type: application/json" \
  -d '{"text":"Fatura #001\nToplam: 49.99 TL\nTeşekkürler!","language":"tr"}'

# Print image (base64)
curl -X POST http://localhost:5160/api/printer/print/image \
  -H "Content-Type: application/json" \
  -d '{"imageBase64":"<base64-string>"}'

# Print QR code
curl -X POST http://localhost:5160/api/printer/print/qr \
  -H "Content-Type: application/json" \
  -d '{"content":"https://example.com/order/12345"}'

# Check status
curl http://localhost:5160/api/printer/status

# View logs
curl http://localhost:5160/api/printer/logs

# Download CSV logs
curl http://localhost:5160/api/printer/logs/export -o logs.csv

# Reprint a failed job
curl -X POST http://localhost:5160/api/printer/reprint \
  -H "Content-Type: application/json" \
  -d '{"jobId":"ABC12345"}'

# Health check
curl http://localhost:5160/health

# Inject PAPER_OUT error (for testing)
curl -X POST "http://localhost:5160/api/printer/simulate/error?code=PAPER_OUT"

# Clear the error
curl -X POST "http://localhost:5160/api/printer/simulate/clear?code=PAPER_OUT"
```

---

## Log Format (JSONL — `logs/printer-logs.json`)

Each line is a JSON object:

```json
{"ts":"2025-10-02T12:34:56Z","op":"print_image","conn":"usb","jobId":"ABC123","status":"error","error":{"code":"PAPER_OUT","detail":"No paper detected"}}
{"ts":"2025-10-02T12:35:10Z","op":"print_text","conn":"lan","jobId":"DEF456","status":"success","error":null}
```

---

## Error Codes

| Code | Meaning | UI Message |
|------|---------|------------|
| `PAPER_OUT` | No paper in printer | "No paper detected" |
| `PAPER_JAM` | Paper jam | "Paper jam detected" |
| `COVER_OPEN` | Printer cover open | "Printer cover is open" |
| `OVERHEAT` | Temperature > 70°C | "Printer overheated — cooling down" |
| `COMM_ERROR` | Connection lost | "Communication error" |
| `UNKNOWN_COMMAND` | Bad command | "Unknown or unsupported command" |

---

## Features

**Core:**
- USB + LAN connection (auto-reconnect with exponential backoff)
- `POST /connect` with live mode switching
- Text, image, and QR printing
- Structured JSONL logs + CSV export
- Failed job storage with **Tekrar Bastır** (Reprint) button

**Bonus:**
- `/health` endpoint (503 when degraded)
- Queue idempotency via `idempotencyKey`
- Retry/backoff (up to 3× with 2ⁿ × 500ms delay)
- Paper roll life estimate (`estimatedPrintsRemaining`)
- Dockerfile + docker-compose
- Simulation helpers to inject/clear hardware errors

---

## Notes

- Physical printer not required — full simulation mode built in.
- Credentials must not be hard-coded; use `.env` or `appsettings.{env}.json`.
- The `logs/` directory is created automatically on first run.
