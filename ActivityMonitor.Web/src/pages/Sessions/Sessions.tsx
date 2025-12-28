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
import TimeRender from "../../components/ui/TimeRender";

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
    {
      header: "Check In",
      key: "checkIn",
      className: "check-in",
      render: (record: SessionRecord) =>
        record.checkIn !== "-" ? (
          <TimeRender
            label={record.checkIn}
            redirect={record.checkInDateIso}
            key={record.checkInDateIso}
          />
        ) : (
          ""
        ),
    },
    {
      header: "Shift In",
      key: "shiftIn",
      className: "shift-in",
      render: (record: SessionRecord) =>
        record.shiftIn !== "-" ? (
          <TimeRender
            label={record.shiftIn}
            redirect={record.shiftInDateIso}
            key={record.shiftInDateIso}
          />
        ) : (
          ""
        ),
    },
    {
      header: "Shift Out",
      key: "shiftOut",
      className: "shift-out",
      render: (record: SessionRecord) =>
        record.shiftOut !== "-" ? (
          <TimeRender
            label={record.shiftOut}
            redirect={record.shiftOutDateIso}
            key={record.shiftOutDateIso}
          />
        ) : (
          ""
        ),
    },
    {
      header: "Check Out",
      key: "checkout",
      className: "checkout",
      render: (record: SessionRecord) =>
        record.checkout !== "-" ? (
          <TimeRender
            label={record.checkout}
            redirect={record.checkoutDateIso}
            key={record.checkoutDateIso}
          />
        ) : (
          ""
        ),
    },
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
                <Card className="day-card" data-shift={day.dataShift}>
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
            {dayData &&
              pagination.currentPage * pagination.pageSize <
                pagination.totalItems && (
                <button
                  className="load-more-days-button"
                  onClick={() =>
                    pagination.onPageChange(pagination.currentPage + 1)
                  }
                >
                  <Card className="day-card">
                    <CardBody>
                      <strong className="content">
                        <span className="material-symbols-outlined">add</span>
                        <span>Load More</span>
                      </strong>
                    </CardBody>
                  </Card>
                </button>
              )}
          </div>
        )}
      </section>
    </>
  );
};

export default Sessions;
