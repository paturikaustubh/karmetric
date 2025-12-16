import React, { useEffect, useState, useRef } from "react";
import LoadingScreen from "../../components/ui/LoadingScreen";
import BarChart from "../../components/ui/Chart";
import Table, { type Column } from "../../components/ui/Table";
import { Link } from "react-router-dom";
import {
  Card,
  CardTitle,
  CardBody,
  CardFooter,
} from "../../components/ui/card";
import styles from "./styles.module.css";
import "./styles.css";

const API_URL = "http://localhost:2369/api/activity";

const Dashboard: React.FC = () => {
  const [loading, setLoading] = useState(true);

  // State
  const [status, setStatus] = useState<any>(null);
  const [today, setToday] = useState<any>({
    sessionCount: 0,
    firstCheckIn: "-",
  });
  const [weekData, setWeekData] = useState<any>(null);
  const [recentSessions, setRecentSessions] = useState<any[]>([]);

  // Timer State
  const [timerString, setTimerString] = useState("0h 0m 0s");
  const intervalRef = useRef<number | null>(null);

  // Format Duration
  const formatDuration = (ms: number) => {
    const totalSeconds = Math.floor(ms / 1000);
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;
    return `${hours}h ${minutes}m ${seconds}s`;
  };

  // Timer Logic
  const updateTimer = (startTimeStr: string | null) => {
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
  };

  // Fetch Functions
  const fetchData = async () => {
    try {
      const [statusRes, todayRes, recentRes, weekRes] = await Promise.all([
        fetch(`${API_URL}/status`).then((r) => r.json()),
        fetch(`${API_URL}/today`).then((r) => r.json()),
        fetch(`${API_URL}/sessions/grid?limit=3`).then((r) => r.json()),
        fetch(`${API_URL}/week`).then((r) => r.json()),
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
  };

  useEffect(() => {
    fetchData();
    const poll = setInterval(fetchData, 60000);
    return () => {
      clearInterval(poll);
      if (intervalRef.current) clearInterval(intervalRef.current);
    };
  }, []);

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

  // Recent Columns
  const recentColumns: Column<any>[] = [
    { header: "Check In", key: "checkIn", className: "check-in" },
    { header: "Check Out", key: "checkout", className: "checkout" },
    { header: "Duration", key: "duration", className: "duration" },
  ];

  return (
    <>
      <LoadingScreen isOpen={loading} />
      <section className={styles.content}>
        {/* Card 1: Current Session */}
        <Card className={styles.card}>
          <CardTitle>Current Session</CardTitle>
          <CardBody>
            <h1 className="current-session">{timerString}</h1>
          </CardBody>
          <CardFooter>
            <div className="session-footer">
              <span>
                <strong>Since:</strong> {getSinceText()}
              </span>
              <span>
                <strong>Today's sessions:</strong> {today.sessionCount}
              </span>
            </div>
          </CardFooter>
        </Card>

        {/* Card 2: Recent Sessions */}
        <Card className={`${styles.card} recent-sessions`}>
          <CardTitle className="recent-sessions">
            <span>Recent Sessions</span>
            <Link to="/sessions">
              <button>View All</button>
            </Link>
          </CardTitle>
          <CardBody style={{ marginBlock: "0.5em", marginBottom: 0 }}>
            <Table
              columns={recentColumns}
              data={recentSessions}
              showPaginator={false}
              getRowProps={(item) => ({ "data-shift": item.dataShift } as any)}
            />
          </CardBody>
        </Card>

        {/* Card 3: Weekly Summary */}
        <Card className={`${styles.card} weekly-summary`}>
          <CardTitle className="weekly-summary">
            <span>Weekly Summary</span>
            <span
              style={{
                fontSize: "0.8em",
                fontWeight: "bold",
                alignSelf: "center",
              }}
            >
              Total: {weekData?.totalDuration || "0h"}
            </span>
          </CardTitle>
          <CardBody>
            {weekData && (
              <BarChart
                data={weekData.chartData}
                xAxisLabels={weekData.chartData.map((d: any) => d.axisLabel)}
              />
            )}
          </CardBody>
        </Card>
      </section>
    </>
  );
};

export default Dashboard;
