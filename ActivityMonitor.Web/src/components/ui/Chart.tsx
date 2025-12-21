import React, { useEffect, useRef, useState } from "react";
import "./Chart.css";

type DataPoint = { label?: string; value: number };
export interface BarChartProps {
  data?: number[] | DataPoint[];
  xAxisLabels?: string[];
  height?: number; // px, default 340
}

export default function BarChart({
  data: rawDataProp,
  xAxisLabels: xAxisLabelsProp,
  height = 340,
}: BarChartProps) {
  const wrapRef = useRef<HTMLDivElement | null>(null);

  const [size, setSize] = useState({ width: 600, height });

  // Tooltip state
  const [tip, setTip] = useState({ visible: false, left: 0, top: 0, text: "" });

  // Processed data
  const [data, setData] = useState<number[]>([]);
  const [dataLabels, setDataLabels] = useState<string[]>([]);
  const [xAxisLabels, setXAxisLabels] = useState<string[]>([]);

  // Initialize/process input data
  useEffect(() => {
    let rawData = rawDataProp as any;

    if (!rawData && data.length > 0) rawData = data;

    if (!rawData) {
      const fallback = [10, 20, 15, 30, 25, 40, 22];
      setData(fallback);
      setDataLabels(fallback.map(String));
      setXAxisLabels(fallback.map((_, i) => String(i + 1)));
      return;
    }

    if (
      Array.isArray(rawData) &&
      rawData.length > 0 &&
      typeof rawData[0] === "object"
    ) {
      const values = (rawData as DataPoint[]).map((d) => d.value);
      const labels = (rawData as DataPoint[]).map(
        (d) => d.label ?? String(d.value)
      );
      setData(values);
      setDataLabels(labels);
    } else if (Array.isArray(rawData)) {
      const values = (rawData as number[]).slice();
      setData(values);
      setDataLabels(values.map(String));
    } else {
      // Unexpected shape -> fallback
      const fallback = [10, 20, 15, 30, 25, 40, 22];
      setData(fallback);
      setDataLabels(fallback.map(String));
    }

    // X axis labels
    if (xAxisLabelsProp) {
      setXAxisLabels(xAxisLabelsProp);
    } else {
      // generate if mismatched
      setXAxisLabels((prev) => {
        const expected =
          (Array.isArray(rawData) ? rawData.length : data.length) ||
          data.length ||
          0;
        if (prev && prev.length === expected) return prev;
        return Array.from({ length: expected }, (_, i) => String(i + 1));
      });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [rawDataProp, xAxisLabelsProp]);

  // Setup ResizeObserver
  useEffect(() => {
    const el = wrapRef.current;
    if (!el) return;

    // set initial
    const rect = el.getBoundingClientRect();
    setSize({ width: rect.width || 600, height: rect.height || height });

    const ro = new ResizeObserver(() => {
      const r = el.getBoundingClientRect();
      setSize({ width: r.width || 600, height: r.height || height });
    });
    ro.observe(el);
    return () => ro.disconnect();
  }, [height]);

  // Tooltip helpers
  const showTip = (e: React.PointerEvent, text: string) => {
    const wrap = wrapRef.current;
    if (!wrap) return;
    const r = wrap.getBoundingClientRect();
    setTip({
      visible: true,
      left: e.clientX - r.left,
      top: e.clientY - r.top - 10,
      text,
    });
  };
  const moveTip = (e: React.PointerEvent) => {
    const wrap = wrapRef.current;
    if (!wrap) return;
    const r = wrap.getBoundingClientRect();
    setTip((t) => ({
      ...t,
      left: e.clientX - r.left,
      top: e.clientY - r.top - 10,
    }));
  };
  const hideTip = () => setTip({ visible: false, left: 0, top: 0, text: "" });

  // Layout constants and rendering calculations
  const pad = { top: 20, right: 20, bottom: 40, left: 40 };
  const width = size.width || 600;
  const heightPx = size.height || height;
  const w = Math.max(0, width - pad.left - pad.right);
  const h = Math.max(0, heightPx - pad.top - pad.bottom);

  // Fix: Ensure maxVal is at least 4 to prevent duplicate ticks (0,0,1,1) for small values
  const rawMax = data.length ? Math.max(...data) : 0;
  const maxVal = Math.max(rawMax, 4);

  const avg = data.length ? data.reduce((a, b) => a + b, 0) / data.length : 0;
  const scaleY = (v: number) => (v / (maxVal || 1)) * h;

  const n = data.length;
  const barW = 45;
  const sidePad = 10;
  const totalBarWidth = barW * n;
  const remaining = w - totalBarWidth - sidePad * 2;
  const gap = n > 1 ? remaining / (n - 1) : 0;

  // Prepare SVG children
  const yTicks = Array.from({ length: 5 }, (_, i) => {
    const val = (maxVal / 4) * i;
    const y = h - scaleY(val);
    return {
      key: `yt-${i}`,
      val: Math.round(val),
      x: -8,
      y: y + 4,
    };
  });

  const avgY = h - scaleY(avg);

  return (
    <div className="bc-wrap" ref={wrapRef} style={{ height: `${height}px` }}>
      <svg
        className="bc-svg"
        viewBox={`0 0 ${width} ${heightPx}`}
        preserveAspectRatio="none"
      >
        <g transform={`translate(${pad.left},${pad.top})`}>
          {/* Y Axis */}
          <line x1={0} y1={0} x2={0} y2={h} stroke="#d1d5db" strokeWidth={2} />
          {/* X Axis */}
          <line x1={0} y1={h} x2={w} y2={h} stroke="#d1d5db" strokeWidth={2} />

          {/* Y ticks */}
          {yTicks.map((t) => (
            <text key={t.key} x={t.x} y={t.y} className="bc-y">
              {t.val}
            </text>
          ))}

          {/* Average visual line */}
          <line
            x1={0}
            x2={w}
            y1={avgY}
            y2={avgY}
            stroke="#ef4444"
            strokeDasharray="5 5"
            strokeWidth={2}
            className="bc-avg-line"
            style={{ pointerEvents: "none" }}
          />

          {/* Average hit area */}
          <line
            x1={0}
            x2={w}
            y1={avgY}
            y2={avgY}
            stroke="transparent"
            strokeWidth={20}
            style={{ cursor: "pointer" }}
            onPointerEnter={(e) => showTip(e, `Average: ${avg.toFixed(2)}`)}
            onPointerMove={(e) => moveTip(e)}
            onPointerLeave={() => hideTip()}
          />

          {/* Bars + X labels */}
          {data.map((val, i) => {
            const barHeight = scaleY(val);
            const x = sidePad + i * (barW + gap);
            const y = h - barHeight;
            const r = val === 0 ? 0 : 6;
            const d = [
              `M ${x},${y + barHeight}`,
              `L ${x},${y + r}`,
              `a ${r},${r} 0 0 1 ${r},-${r}`,
              `L ${x + barW - r},${y}`,
              `a ${r},${r} 0 0 1 ${r},${r}`,
              `L ${x + barW},${y + barHeight}`,
              "Z",
            ].join(" ");

            return (
              <g key={`bar-${i}`}>
                <path
                  d={d}
                  fill="#3b82f6"
                  className="bc-bar"
                  onPointerEnter={(e) =>
                    showTip(e, dataLabels[i] ?? String(val))
                  }
                  onPointerMove={(e) => moveTip(e)}
                  onPointerLeave={() => hideTip()}
                />

                <text x={x + barW / 2} y={h + 18} className="bc-x">
                  {xAxisLabels[i]
                    ? xAxisLabels[i].split(/\r?\n/).map((line, li) => (
                        <tspan
                          key={li}
                          x={x + barW / 2}
                          dy={li === 0 ? 0 : 14}
                          className="bc-x-tspan"
                        >
                          {line}
                        </tspan>
                      ))
                    : String(i + 1)}
                </text>
              </g>
            );
          })}
        </g>
      </svg>

      <div
        className="bc-tip"
        style={{
          display: tip.visible ? "block" : "none",
          left: tip.left + "px",
          top: tip.top + "px",
        }}
      >
        {tip.text}
      </div>
    </div>
  );
}
