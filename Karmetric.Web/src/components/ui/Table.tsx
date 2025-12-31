import React from "react";
import "./Table.css";
import "./Paginator.module.css"; // Ensure paginator styles if needed or remove if unused in Table itself.
import Paginator from "./Paginator";

export interface Column<T> {
  header: string;
  key: keyof T | string; // Accessor key
  render?: (item: T, index: number) => React.ReactNode;
  className?: string;
}

interface TableProps<T> {
  data: T[];
  columns: Column<T>[];
  keyExtractor?: (item: T, index: number) => string | number;
  showPaginator?: boolean;
  // Paginator props
  pagination?: PaginatorProps;
  reverseShifts?: boolean;
  // Special prop for the Session "Shift" visualization
  getRowProps?: (item: T) => React.HTMLAttributes<HTMLTableRowElement>;
}

function Table<T>({
  data,
  columns,
  keyExtractor = (_, i) => i,
  showPaginator = false,
  pagination,
  reverseShifts = false,
  getRowProps,
}: TableProps<T>) {
  return (
    <div className="table-paginator-container">
      <table className={`sessions ${reverseShifts ? "reverse-shifts" : ""}`}>
        <thead>
          <tr>
            {columns.map((col, i) => (
              <th key={i}>
                <span>{col.header}</span>
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {data.map((item, index) => {
            const rowProps = getRowProps ? getRowProps(item) : {};
            return (
              <tr key={keyExtractor(item, index)} {...rowProps}>
                {columns.map((col, colIndex) => (
                  <td key={colIndex} className={col.className}>
                    {col.render
                      ? col.render(item, index)
                      : (item as unknown as Record<string, React.ReactNode>)[
                          col.key as string
                        ]}
                  </td>
                ))}
              </tr>
            );
          })}
        </tbody>
      </table>

      {showPaginator && pagination && (
        <Paginator
          currentPage={pagination.currentPage}
          totalPages={pagination.totalPages}
          totalItems={pagination.totalItems}
          pageSize={pagination.pageSize}
          onPageChange={pagination.onPageChange}
          onPageSizeChange={pagination.onPageSizeChange}
        />
      )}
    </div>
  );
}

export default Table;
