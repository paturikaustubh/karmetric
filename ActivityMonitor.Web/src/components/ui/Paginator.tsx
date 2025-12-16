import styles from "./Paginator.module.css";

interface PaginatorProps {
  currentPage: number;
  pageSize: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
}

export default function Paginator({
  currentPage,
  pageSize,
  totalPages,
  onPageChange,
  onPageSizeChange,
}: PaginatorProps) {
  const handlePageInput = (val: number) => {
    if (val < 1) val = 1;
    if (val > totalPages) val = totalPages;
    if (val !== currentPage) onPageChange(val);
  };

  return (
    <div id="paginator">
      <div className={styles.paginationContainer}>
        {/* LEFT SIDE */}
        <div className={styles.leftControls}>
          <span className={styles.rowsLabel}>Rows per page:</span>
          <select
            className={styles.pageSizeSelect}
            value={pageSize}
            onChange={(e) => onPageSizeChange(parseInt(e.target.value))}
          >
            {[5, 10, 20, 50, 100].map((size) => (
              <option key={size} value={size}>
                {size}
              </option>
            ))}
          </select>
        </div>
        {/* RIGHT SIDE */}
        <div className={styles.rightControls}>
          <button
            className={styles.iconBtn}
            disabled={currentPage === 1}
            title="First Page"
            onClick={currentPage !== 1 ? () => onPageChange(1) : undefined}
          >
            <span className="material-symbols-outlined">first_page</span>
          </button>
          <button
            className={styles.iconBtn}
            disabled={currentPage === 1}
            title="Previous Page"
            onClick={
              currentPage !== 1
                ? () => onPageChange(currentPage - 1)
                : undefined
            }
          >
            <span className="material-symbols-outlined">chevron_left</span>
          </button>
          <input
            type="number"
            className={styles.pageInput}
            value={currentPage}
            min={1}
            max={totalPages}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                const val = Number(e.currentTarget.value);
                handlePageInput(val);
              }
            }}
            onBlur={(e) => {
              const val = Number(e.currentTarget.value);
              handlePageInput(val);
            }}
            onChange={() => {}}
          />
          <span className={styles.pageLabel}>/ {totalPages}</span>
          <button
            className={styles.iconBtn}
            disabled={currentPage === totalPages}
            title="Next Page"
            onClick={
              currentPage !== totalPages
                ? () => onPageChange(currentPage + 1)
                : undefined
            }
          >
            <span className="material-symbols-outlined">chevron_right</span>
          </button>
          <button
            className={styles.iconBtn}
            disabled={currentPage === totalPages}
            title="Last Page"
            onClick={
              currentPage !== totalPages
                ? () => onPageChange(totalPages)
                : undefined
            }
          >
            <span className="material-symbols-outlined">last_page</span>
          </button>
        </div>
      </div>
    </div>
  );
}
