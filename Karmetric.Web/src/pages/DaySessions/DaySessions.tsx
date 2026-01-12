import { Link } from "react-router-dom";
import LoadingScreen from "../../components/ui/LoadingScreen";
import type { Column } from "../../components/ui/Table";
import Table from "../../components/ui/Table";
import useDaySessions from "../../hooks/useDaySessions";
import "./styles.css";

export default function DaySessions() {
  const { loading, sessions, summary, pagination, navigators } =
    useDaySessions();
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

  /* Helper to render disabled button if link is null */
  const renderNavButton = (
    date: string | null,
    icon: string,
    label: string
  ) => {
    if (date) {
      return (
        <Link to={`/sessions/days/${date}`} aria-label={label}>
          <button className="material-symbols-outlined">{icon}</button>
        </Link>
      );
    }
    return (
      <Link to={`#`}>
        <button className="material-symbols-outlined" disabled>
          {icon}
        </button>
      </Link>
    );
  };

  return (
    <>
      <LoadingScreen isOpen={loading} />
      <section className="page-title">
        <div className="page-title-left">
          <Link to="/sessions/days">
            <button>
              <span className="material-symbols-outlined">arrow_back</span>
            </button>
          </Link>
          <h2>{summary && `${summary.date} - ${summary.day}`}</h2>
        </div>
        <div className="page-title-right">
          <div className="navigators">
            {renderNavButton(
              navigators.previousDate,
              "chevron_left",
              "Previous Day"
            )}
            {renderNavButton(navigators.nextDate, "chevron_right", "Next Day")}
          </div>
          <h2>{summary && `${summary.totalDuration}`}</h2>
        </div>
      </section>
      <section className="content">
        <Table
          reverseShifts={true}
          columns={columns}
          data={sessions}
          showPaginator={true}
          pagination={pagination}
          getRowProps={(item) =>
            ({
              "data-shift": item.dataShift,
            } as React.HTMLAttributes<HTMLTableRowElement>)
          }
        />
      </section>
    </>
  );
}
