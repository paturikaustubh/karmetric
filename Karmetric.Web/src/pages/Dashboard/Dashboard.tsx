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
import TimeRender from "../../components/ui/TimeRender";
import useDashboard from "../../hooks/useDashboard";

const Dashboard: React.FC = () => {
  const {
    loading,
    timerString,
    getSinceText,
    today,
    weekData,
    recentSessions,
    nextWeek,
    prevWeek,
  } = useDashboard();
  const recentColumns: Column<SessionRecord>[] = [
    {
      header: "Check In",
      key: "checkIn",
      className: "check-in",
      render: (record: SessionRecord) => (
        <TimeRender
          label={record.checkIn}
          redirect={record.checkInDateIso}
          key={record.checkInDateIso}
        />
      ),
    },
    {
      header: "Check Out",
      key: "checkout",
      className: "checkout",
      render: (record: SessionRecord) => (
        <TimeRender
          label={record.checkout}
          redirect={record.checkoutDateIso}
          key={record.checkoutDateIso}
        />
      ),
    },
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
              getRowProps={(item) =>
                ({
                  "data-shift": item.dataShift,
                } as React.HTMLAttributes<HTMLTableRowElement>)
              }
            />
          </CardBody>
        </Card>

        {/* Card 3: Weekly Summary */}
        <Card className={`${styles.card} weekly-summary`}>
          <CardTitle className="weekly-summary">
            <div className="left-title">
              <span>Weekly Summary</span>
              <div className="navigators">
                <button
                  onClick={prevWeek}
                  disabled={!weekData || weekData.isFirstWeek}
                >
                  <span className="material-symbols-outlined">
                    chevron_left
                  </span>
                </button>
                <button
                  onClick={nextWeek}
                  disabled={!weekData || weekData.isLatestWeek}
                >
                  <span className="material-symbols-outlined">
                    chevron_right
                  </span>
                </button>
              </div>
            </div>
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
                xAxisLabels={weekData.chartData.map(
                  (d: ChartDataPoint) => d.axisLabel
                )}
              />
            )}
          </CardBody>
        </Card>
      </section>
    </>
  );
};

export default Dashboard;
