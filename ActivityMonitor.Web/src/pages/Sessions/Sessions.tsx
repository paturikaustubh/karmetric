import LoadingScreen from "../../components/ui/LoadingScreen";
import { Link } from "react-router-dom";
import Table, { type Column } from "../../components/ui/Table";
import "./styles.css";
import useSessions from "../../hooks/useSessions";
import {
  Card,
  CardBody,
  CardFooter,
  CardTitle,
} from "../../components/ui/card";

const Sessions: React.FC = () => {
  const { gridData, dayData, loading, pagination, layout, handleLayoutChange } =
    useSessions();

  const columns: Column<SessionRecord>[] = [
    {
      header: "S No.",
      key: "sno",
      render: (_: SessionRecord, index: number) => {
        const startIndex = (pagination.currentPage - 1) * pagination.pageSize;
        return <span>{startIndex + index + 1}</span>;
      },
      className: "sno",
    },
    { header: "Check In", key: "checkIn", className: "check-in" },
    { header: "Shift In", key: "shiftIn", className: "shift-in" },
    { header: "Shift Out", key: "shiftOut", className: "shift-out" },
    { header: "Check Out", key: "checkout", className: "checkout" },
    {
      header: "Duration",
      key: "duration",
      className: "duration",
      render: (item: SessionRecord) => <span>{item.duration}</span>,
    },
  ];
  return (
    <>
      <LoadingScreen isOpen={loading} />
      <section className="page-title">
        <div className="page-title-left">
          <Link to="/">
            <button>
              <span className="material-symbols-outlined">arrow_back</span>
            </button>
          </Link>
          <h2>Sessions</h2>
        </div>
        <div className="button-group layout-buttons">
          <button
            className={layout === "grid" ? "active" : ""}
            onClick={() => handleLayoutChange("grid")}
          >
            <span className="material-symbols-outlined">table_rows_narrow</span>
          </button>
          <button
            className={layout === "days" ? "active" : ""}
            onClick={() => handleLayoutChange("days")}
          >
            <span className="material-symbols-outlined">calendar_month</span>
          </button>
        </div>
      </section>
      <section className="content">
        {layout === "grid" ? (
          <Table
            columns={columns}
            data={gridData}
            showPaginator={true}
            pagination={pagination}
            getRowProps={(item) =>
              ({
                "data-shift": item.dataShift,
              } as React.HTMLAttributes<HTMLTableRowElement>)
            }
          />
        ) : (
          <div className="days-cards">
            {dayData.map((day) => (
              <Link to={`/sessions/days/${day.dateIso}`} key={day.dateIso}>
                <Card className={`day-card ${day.dataShift}`}>
                  <CardTitle>
                    <span>{day.date}</span>
                    <span className="day">{day.day}</span>
                  </CardTitle>
                  <CardBody>
                    <span>
                      <strong>Duration:</strong> {day.totalDuration}
                    </span>
                  </CardBody>
                  <CardFooter>
                    <span>
                      <strong>Sessions:</strong> {day.sessionCount}
                    </span>
                  </CardFooter>
                </Card>
              </Link>
            ))}
          </div>
        )}
      </section>
    </>
  );
};

export default Sessions;
