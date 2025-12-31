# Karmetric - Web UI

The frontend interface for Karmetric, built with **React**, **TypeScript**, and **Vite**.

## 🚀 Overview

This Single Page Application (SPA) serves as the user interface for the Karmetric. It connects to the local Background Service API to display real-time status and historical data.

### Tech Stack

- **Framework**: React 19
- **Build Tool**: Vite 7
- **Language**: TypeScript
- **Routing**: React Router DOM 7
- **Styling**: Vanilla CSS (Scoped Modules + Global Variables)

## ✨ Features

- **Dashboard**: Real-time status (In/Away/Idle), daily summary, and recent sessions.
- **Sessions Grid**: detailed list of all sessions with pagination.
- **Day View**: Detailed timeline and session breakdown for any specific day.
- **Visualizations**: "Shift" indicators (J/7 shapes) for sessions spanning across days.

## 🛠️ Development

### Prerequisites

- Node.js (v20+)
- npm

### Setup

1.  Navigate to this directory:
    ```bash
    cd Karmetric.Web
    ```
2.  Install dependencies:
    ```bash
    npm install
    ```

### Running Locally

```bash
npm run dev
```

> **Note**: This application expects the **Background Service** API to be running at `http://localhost:2369`. If the backend is not running, API calls will fail.

### Building for Production

```bash
npm run build
```

This generates static files in `dist/`. These files are then copied to the Background Service for hosting during the full application build process.

## 📁 Project Structure

- `src/pages`: Main view components (Dashboard, Sessions, DaySessions).
- `src/components`: Reusable UI components (Table, Navbar, etc.).
- `src/hooks`: Custom React hooks for API data fetching.
- `src/types`: TypeScript definitions for API responses.
