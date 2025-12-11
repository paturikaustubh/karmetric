import { BarChart } from "./components/ui/chart.js";
import LoadingScreen from "./components/ui/LoadingScreen.js";

const API_URL = "http://localhost:2369/api/activity";

// DOM Elements
const currentSessionEl = document.querySelector("h1.current-session");
const sessionSinceEl = document.querySelector(
  ".session-footer span:first-child"
);
const sessionCountEl = document.querySelector(
  ".session-footer span:last-child"
);
const weeklyTotalEl = document.querySelector(
  ".card-title.weekly-summary span:last-child"
);

let chartInstance = null;
let timerInterval = null;

const formatDuration = (ms) => {
  const totalSeconds = Math.floor(ms / 1000);
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;
  return `${hours}h ${minutes}m ${seconds}s`;
};

const updateTimer = (startTimeStr) => {
  // If no start time (e.g. idle/stopped), reset to 0
  if (!startTimeStr || startTimeStr === "-") {
    currentSessionEl.textContent = "0h 0m 0s";
    if (timerInterval) clearInterval(timerInterval);
    return;
  }

  const start = new Date(startTimeStr).getTime();

  // Clear existing interval to restart cleanly
  if (timerInterval) clearInterval(timerInterval);

  // Immediate update
  const now = new Date().getTime();
  currentSessionEl.textContent = formatDuration(now - start);

  // Tick every second using local clock
  timerInterval = setInterval(() => {
    const now = new Date().getTime();
    const diff = now - start;
    currentSessionEl.textContent = formatDuration(diff);
  }, 1000);
};

const fetchStatus = async () => {
  try {
    const res = await fetch(`${API_URL}/status`);
    const data = await res.json();

    if (data.status === "In") {
      // data.startTime is ISO string.
      updateTimer(data.startTime);

      // FIX: Update "Since" footer to show CURRENT session start
      const startDate = new Date(data.startTime);
      // Format: "5 Dec, 25 - 17:32"
      const dateStr =
        startDate.toLocaleDateString("en-GB", {
          day: "numeric",
          month: "short",
          year: "2-digit",
        }) +
        " - " +
        startDate.toLocaleTimeString("en-GB", {
          hour: "2-digit",
          minute: "2-digit",
        });

      sessionSinceEl.innerHTML = `<strong>Since:</strong> ${dateStr}`;
    } else {
      updateTimer(null);
      // Ideally show last checkout? For now, fetchToday handles the default "First CheckIn" if we are OUT.
    }
  } catch (e) {
    console.error("Status fetch failed", e);
  }
};

const fetchToday = async () => {
  try {
    const res = await fetch(`${API_URL}/today`);
    const data = await res.json();

    // Only set "Since" if NOT IN (or if fetchStatus hasn't run yet).
    // But fetchStatus runs frequently. Let's let fetchStatus override it if IN.
    // If we are OUT, this shows first checkin of day.
    if (currentSessionEl.textContent === "0h 0m 0s") {
      sessionSinceEl.innerHTML = `<strong>Since:</strong> ${data.date} - ${data.firstCheckIn}`;
    }

    // "Today's sessions: 4"
    sessionCountEl.innerHTML = `<strong>Today's sessions:</strong> ${data.sessionCount}`;
  } catch (e) {
    console.error("Today fetch failed", e);
  }
};

const fetchRecentSessions = async () => {
  try {
    const res = await fetch(`${API_URL}/sessions?limit=3`); // Get 3 recent (excluding current)
    const json = await res.json();
    const sessions = json.data;

    const tbody = document.querySelector(".card.recent-sessions table tbody");
    if (!tbody) return;
    tbody.innerHTML = "";

    sessions.forEach((s) => {
      const tr = document.createElement("tr");
      tr.setAttribute("data-shift", s.dataShift || "");

      // Format duration bracket if needed (not supported in DTO yet fully but let's use raw duration)
      // DTO duration is "Xh Ym".

      tr.innerHTML = `
                <td class="check-in"><span>${s.checkIn}</span></td>
                <td class="checkout"><span>${s.checkout}</span></td>
                <td class="duration"><span>${s.duration}</span></td>
            `;
      tbody.appendChild(tr);
    });
  } catch (e) {
    console.error("Recent sessions fetch failed", e);
  }
};

const fetchWeek = async () => {
  try {
    const res = await fetch(`${API_URL}/week`);
    const data = await res.json();

    weeklyTotalEl.textContent = `Total: ${data.totalDuration}`;

    if (!chartInstance) {
      chartInstance = new BarChart("weekly-summary-bar-chart", {
        data: data.chartData, // [{ value, label, axisLabel }]
        xAxisLabels: data.chartData.map((d) => d.axisLabel),
      });
    } else {
      chartInstance.update({
        data: data.chartData,
        xAxisLabels: data.chartData.map((d) => d.axisLabel),
      });
    }
  } catch (e) {
    console.error("Week fetch failed", e);
  }
};

const init = async () => {
  // Initial Load: Show Loading Screen and fetch all in parallel
  try {
    LoadingScreen(true);
    await Promise.all([
      fetchStatus(),
      fetchToday(),
      fetchWeek(),
      fetchRecentSessions(),
    ]);
  } catch (error) {
    console.error("Initialization failed", error);
  } finally {
    LoadingScreen(false);
  }

  // Poll status less frequently since timer is local (e.g. 60s to check for sync/stops)
  setInterval(async () => {
    await Promise.all([fetchStatus(), fetchToday(), fetchRecentSessions()]);
  }, 60000); // 1 minute

  // Poll chart less often
  setInterval(fetchWeek, 300000); // 5 mins
};

document.addEventListener("DOMContentLoaded", init);
