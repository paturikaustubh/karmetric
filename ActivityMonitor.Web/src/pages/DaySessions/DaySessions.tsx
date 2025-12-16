import { Link } from "react-router-dom";
import LoadingScreen from "../../components/ui/LoadingScreen";
import type { Column } from "../../components/ui/Table";
import Table from "../../components/ui/Table";
import useDaySessions from "../../hooks/useDaySessions";
import "./styles.css";

export default function DaySessions() {
  const {
    loading,
    sessions,
    pagination,
    handlePageChange,
    handlePageSizeChange,
  } = useDaySessions();
  const columns: Column<SessionRecord>[] = [
    {
      header: "S No.",
      key: "sno",
      render: (_: SessionRecord, index: number) => {
        const startIndex = (pagination.page - 1) * pagination.pageSize;
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
          <Link to="/sessions">
            <button>
              <span className="material-symbols-outlined">arrow_back</span>
            </button>
          </Link>
          <h2>Sessions</h2>
        </div>
      </section>
      <section className="content">
        <Table
          reverseShifts={true}
          columns={columns}
          data={sessions}
          showPaginator={true}
          pagination={{
            currentPage: pagination.page,
            totalPages: pagination.totalPages,
            pageSize: pagination.pageSize,
            onPageChange: handlePageChange,
            onPageSizeChange: handlePageSizeChange,
          }}
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
