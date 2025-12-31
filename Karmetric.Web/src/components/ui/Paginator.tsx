import { useState, useEffect } from "react";
import styles from "./Paginator.module.css";

export default function Paginator({
  currentPage,
  pageSize,
  totalPages,
  totalItems,
  onPageChange,
  onPageSizeChange,
}: PaginatorProps) {
  const [inputVal, setInputVal] = useState(currentPage.toString());

  useEffect(() => {
    setInputVal(currentPage.toString());
  }, [currentPage]);

  const handlePageInput = (val: number) => {
    if (isNaN(val)) {
      setInputVal(currentPage.toString());
      return;
    }
    let newValue = val;
    if (newValue < 1) newValue = 1;
    if (newValue > totalPages) newValue = totalPages;

    setInputVal(newValue.toString());
    if (newValue !== currentPage) {
      onPageChange(newValue);
    }
  };

  return (
    <div id="paginator">
      <div className={styles.paginationContainer}>
        {/* LEFT SIDE */}
        <div>
          <span>Showing </span>
          <span>
            {(currentPage - 1) * pageSize + 1}-
            {Math.min(currentPage * pageSize, totalItems)} of {totalItems}
          </span>
        </div>
        {/* RIGHT SIDE */}
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
              type="text"
              className={styles.pageInput}
              value={inputVal}
              onKeyDown={(e) => {
                if (e.key === "Enter") {
                  const val = parseInt(e.currentTarget.value, 10);
                  handlePageInput(val);
                  e.currentTarget.blur();
                }
              }}
              onBlur={(e) => {
                const val = parseInt(e.currentTarget.value, 10);
                handlePageInput(val);
              }}
              onChange={(e) => setInputVal(e.target.value)}
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
    </div>
  );
}
