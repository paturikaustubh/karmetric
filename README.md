# Karmetric

**Karmetric** is a background service and modern web application designed to intelligently track working sessions.

[![Download Latest Installer](https://img.shields.io/github/v/release/paturikaustubh/karmetric?label=Download%20Installer&style=for-the-badge&color=blue)](https://github.com/paturikaustubh/karmetric/releases/latest/download/Karmetric.Installer.exe)

## 🚀 Key Features

- **Idle Detection**: Marks you as "Away" if your keyboard/mouse are inactive for 5 minutes.
- **Automatic Check In**: Starts a session when you return from being idle.
- **Midnight Aware**: Correctly handles sessions that span across midnight, splitting them into two logical daily records with "Shift In/Out" indicators.
- **Modern Web UI**: interactive Dashboard and Sessions grid with daily visualizations.
- **Offline First**: Stores all session data in a local SQLite database.

## 🛠️ Architecture

The solution uses a modern architecture running entirely locally on your machine:

1.  **Background Service (.NET 8 Worker)**:

    - Runs silently as a Windows Service.
    - Monitors Input.
    - Manages the SQLite database (`activity.db`).
    - Hosts the **Web UI** and exposes a local REST API at `http://localhost:2369`.

2.  **Web Application (React + Vite)**:

    - A modern Single Page Application (SPA).
    - Served directly by the Background Service.
    - Accessible via your browser at `http://localhost:2369`.
    - Features Dashboard summaries, charts, and detailed session logs.

3.  **Installer (WPF .NET 4.8)**:
    - Custom installer for maximum compatibility.
    - Registers the app to run at startup.
    - Manages configuration settings.

## 📦 Installation

1.  Download the latest `Karmetric.Installer.exe` from the link above (or the [Releases](https://github.com/paturikaustubh/karmetric/releases) page).
2.  Run the installer.
3.  Choose your settings:
    **Strict Monitor**: Standard mouse/keyboard check.
4.  The app will start automatically. Open `http://localhost:2369` in your browser to view your activity.

## 💻 Development

### Prerequisites

- .NET 8 SDK
- Node.js & npm (for Web App)
- PowerShell

### Building Locally

Use the included build script to package everything (Web App + Background + Installer):

```powershell
.\package_installer.ps1
```

This script will:

1. Build the React Web App (`npm run build`).
2. Copy the build artifacts to the Background Service.
3. Publish the Background Service.
4. Build the Installer.
5. Create the final executable in `Karmetric.Installer\bin\Release\net48\`.

### Update Process

The project uses `monitor.json` as the Single Source of Truth for versions.

## 🤝 Contributing

We welcome contributions! To contribute to Karmetric, please follow these steps:

1.  **Fork the Repository**: Fork the project to your GitHub account.
2.  **Clone Your Fork**: Clone your forked repository to your local machine.
3.  **Create a Feature Branch**: Create a new branch for your feature or bug fix.
4.  **Implement Your Changes**: Make your desired code changes.
5.  **Commit Your Changes**: Commit your changes with a clear and descriptive message.
6.  **Push to Your Fork**: Push your new branch to your forked repository on GitHub.
7.  **Open a Pull Request**: Open a Pull Request from your feature branch to the `dev` branch of the original repository.
8.  **Review and Merge**: Your Pull Request will be reviewed, and upon approval and merging, a new version will be automatically released.
