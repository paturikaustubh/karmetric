import { useCallback, useEffect, useRef, useState } from "react";
import { API_URL } from "../constants";

export default function useDashboard() {
  const formatDuration = (ms: number) => {
    const totalSeconds = Math.floor(ms / 1000);
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;
    return `${hours}h ${minutes}m ${seconds}s`;
  };

  const [loading, setLoading] = useState(true);

  // State
  const [status, setStatus] = useState<StatusResponse | null>(null);
  const [today, setToday] = useState<TodaySummary>({
    date: "-",
    sessionCount: 0,
    totalDuration: "0h 0m",
    firstCheckIn: "-",
    latestCheckOut: "-",
  });
  const [weekData, setWeekData] = useState<WeekSummary | null>(null);
  const [recentSessions, setRecentSessions] = useState<SessionRecord[]>([]);

  const [weekOffset, setWeekOffset] = useState(0);

  // Timer State
  const [timerString, setTimerString] = useState("0h 0m 0s");
  const intervalRef = useRef<number | null>(null);

  // Timer Logic
  const updateTimer = useCallback((startTimeStr: string | null) => {
    if (!startTimeStr || startTimeStr === "-") {
      setTimerString("0h 0m 0s");
      if (intervalRef.current) window.clearInterval(intervalRef.current);
      return;
    }

    const start = new Date(startTimeStr).getTime();
    if (intervalRef.current) window.clearInterval(intervalRef.current);

    // Immediate
    const now = new Date().getTime();
    setTimerString(formatDuration(now - start));

    // Interval
    intervalRef.current = window.setInterval(() => {
      const now = new Date().getTime();
      setTimerString(formatDuration(now - start));
    }, 1000);
  }, []);

  // Fetch Functions
  const fetchData = useCallback(async () => {
    try {
      const [statusRes, todayRes, recentRes, weekRes] = await Promise.all([
        fetch(`${API_URL}/status`).then((r) => r.json()),
        fetch(`${API_URL}/today`).then((r) => r.json()),
        fetch(`${API_URL}/sessions/grid?limit=3`).then((r) => r.json()),
        fetch(`${API_URL}/week?offset=${weekOffset}`).then((r) => r.json()),
      ]);

      // Handle Status & Timer
      setStatus(statusRes);
      if (statusRes.status === "In") {
        updateTimer(statusRes.startTime);
      } else {
        updateTimer(null);
      }

      setToday(todayRes);
      setRecentSessions(recentRes.data);
      setWeekData(weekRes);
    } catch (e) {
      console.error("Dashboard fetch failed", e);
    } finally {
      setLoading(false);
    }
  }, [updateTimer, weekOffset]);

  useEffect(() => {
    fetchData();
    const poll = setInterval(fetchData, 60000);
    return () => {
      clearInterval(poll);
      if (intervalRef.current) clearInterval(intervalRef.current);
    };
  }, [fetchData]);

  // Handlers
  const nextWeek = () => setWeekOffset((prev) => prev + 1);
  const prevWeek = () => setWeekOffset((prev) => prev - 1);

  // Helper: "Since" Text
  const getSinceText = () => {
    if (status?.status === "In" && status.startTime) {
      const d = new Date(status.startTime);
      const dateStr =
        d.toLocaleDateString("en-GB", {
          day: "numeric",
          month: "short",
          year: "2-digit",
        }) +
        " - " +
        d.toLocaleTimeString("en-GB", { hour: "2-digit", minute: "2-digit" });
      return dateStr;
    }
    if (today.firstCheckIn) {
      return `${today.date} - ${today.firstCheckIn}`;
    }
    return "-";
  };

  return {
    loading,
    timerString,
    getSinceText,
    today,
    weekData,
    recentSessions,
    nextWeek,
    prevWeek,
    weekOffset,
  };
}
