# Activity Monitor

**Activity Monitor** is a background service and UI application designed to intelligently track working sessions. It solves the common problem of "premature timeout" during inactive periods (like reading documents, watching videos, or sitting in a silent call) by using audio stream detection and power management request monitoring.

## 🚀 Key Features

- **Smart Idle Detection**: Doesn't just check for mouse movements. It listens for active audio streams (Teams calls, Zoom, Videos) and "Keep Awake" power requests (Presentations, Fullscreen Video).
- **Automatic Session Management**: Automatically checks you in when you start working and checks you out when you are away.
- **Midnight Aware**: Correctly handles sessions that span across midnight, splitting them into two logical daily records.
- **Offline First**: Stores all session data in a local SQLite database.
- **No "Time Travel" Bugs**: Robust logic handles system sleep/wake cycles to prevent corrupt data entries.

## 🛠️ Architecture

The solution maps to a standard client-server architecture, but running locally on your machine:

1.  **Background Service (.NET 8 Worker)**:

    - Runs silently as a Windows Service.
    - Monitors Audio Endpoint API and PowerCfg requests.
    - Manages the SQLite database (`activity.db`).
    - Exposes a local REST API (`localhost:2369`) for the UI.

2.  **UI Application (WPF + WebView2)**:

    - A lightweight wrapper around a modern Web App.
    - Displays session history, charts, and current status.
    - Communicates with the Background Service via REST.

3.  **Installer**:
    - Custom `.NET 4.8` installer (for maximum compatibility).
    - Registers the app to run at startup.
    - Manages strict/smart monitoring configuration.

## 📦 Installation

1.  Download the latest `ActivityMonitor.Installer.exe` from the [Releases](tags) page.
2.  Run the installer.
3.  Choose your settings:
    - **Strict Monitor**: Standard mouse/keyboard check.
    - **Smart Monitor** (Default): Includes Audio/Power detection.
4.  The app will start automatically.

## 💻 Development

### Prerequisites

- .NET 8 SDK
- PowerShell

### Building Locally

Use the included build script to package everything:

```powershell
.\package_installer.ps1
```

This will produce an installer in `ActivityMonitor.Installer\bin\Release\net48\`.

### Update Process

The project uses `monitor.json` as the Single Source of Truth for versions.

1.  Update version in `monitor.json`.
2.  Commit and Push to `main`.
3.  GitHub Actions will automatically build and tag a new release.
