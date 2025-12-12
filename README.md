# Activity Monitor

**Activity Monitor** is a background service and UI application designed to intelligently track working sessions. It solves the common problem of "premature timeout" during inactive periods (like reading documents, watching videos, or sitting in a silent call) by using audio stream detection and power management request monitoring.

[![Download Latest Installer](https://img.shields.io/github/v/release/paturikaustubh/activity-monitor?label=Download%20Installer&style=for-the-badge&color=blue)](https://github.com/paturikaustubh/activity-monitor/releases/latest/download/ActivityMonitor.Installer.exe)

## 🚀 Key Features

- **Idle Detection**: Marks you as "Away" if your keyboard/mouse are inactive for 5 minutes.
- **Automatic Check In**: Starts a session when you return from being idle.
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

1.  Download the latest `ActivityMonitor.Installer.exe` from the link above (or the [Releases](https://github.com/paturikaustubh/activity-monitor/releases) page).
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

## 🤝 Contributing

We welcome contributions! To contribute to Activity Monitor, please follow these steps:

1.  **Fork the Repository**: Fork the project to your GitHub account.
2.  **Clone Your Fork**: Clone your forked repository to your local machine.
3.  **Create a Feature Branch**: Create a new branch for your feature or bug fix.
4.  **Implement Your Changes**: Make your desired code changes.
5.  **Commit Your Changes**: Commit your changes with a clear and descriptive message.
6.  **Push to Your Fork**: Push your new branch to your forked repository on GitHub.
7.  **Open a Pull Request**: Open a Pull Request from your feature branch to the `dev` branch of the original repository.
8.  **Review and Merge**: Your Pull Request will be reviewed, and upon approval and merging, a new version will be automatically released.
