# Karmetric - Background Service

The core engine of Karmetric, built as a **.NET 8 Windows Worker Service**.

## 🧠 Overview

This service runs silently in the background to monitor user activity, manage session logic, and host the web interface. It allows the application to function "offline-first" without external servers.

### Tech Stack

- **Framework**: .NET 8 (Worker SDK)
- **API**: ASP.NET Core (Kestrel) inside the Worker
- **Database**: SQLite
- **ORM**: Dapper
- **Native APIs**: Windows Core Audio API, Power Management API

## ⚙️ Core Responsibilities

1.  **Activity Monitoring (`ActivityService.cs`)**:
    - Periodically checks mouse/keyboard input idle time (via `GetLastInputInfo`).
    - Checks for implicit activity via "Smart Monitoring":
      - **Audio**: Detects if audio is playing (e.g., during a call or video) using WASAPI.
      - **Power**: Detects if an app has requested `SystemRequired` or `DisplayRequired` power availability (e.g., presentation mode).
2.  **Data Persistence (`DatabaseService.cs`)**:
    - Manages the SQLite database at `%LocalAppData%\Karmetric\ActivityLog.db`.
    - Handles session start/end logic, including midnight splits.
3.  **API Hosting**:
    - Hosts a local REST API at `http://localhost:2369`.
    - Serves the static files for the **Web UI**.

## 🛠️ Development

### Prerequisites

- .NET 8 SDK

### Running Locally

```bash
cd Karmetric.Background
dotnet run
```

This will:

- Start the Kestrel web server on port `2369`.
- Initialize/Connect to the SQLite database.
- Begin the monitoring loop.

### Configuration

The service can be configured via `appsettings.json` (though most runtime config is currently handled via the Installer/Registry in the deployed version).

### Database

The database file is located at:
`C:\Users\<YourUser>\AppData\Local\Karmetric\ActivityLog.db`

You can open this file with any SQLite viewer (e.g., "DB Browser for SQLite") to inspect raw data.

## 📁 Project Structure

- `Services/`: Core business logic (`ActivityService`, `DatabaseService`).
- `Controllers/`: API Endpoints for the frontend.
- `Models/`: Data transfer objects and database entities.
- `Utils/`: Native interop wrappers (`AudioUtils`, `PowerUtils`).
