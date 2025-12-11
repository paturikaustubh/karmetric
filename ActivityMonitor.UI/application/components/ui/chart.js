export class BarChart {
  constructor(containerId, opts = {}) {
    this.wrap = document.getElementById(containerId);
    if (!this.wrap) throw new Error("Invalid container ID");

    this.#processData(opts);

    // Auto inject all CSS needed 👇
    this.#injectCSS();

    // Create SVG + Tooltip elements
    this.wrap.innerHTML = `
      <div class="bc-wrap">
        <svg class="bc-svg"></svg>
        <div class="bc-tip"></div>
      </div>
    `;

    this.svg = this.wrap.querySelector(".bc-svg");
    this.tooltip = this.wrap.querySelector(".bc-tip");

    this.#setupResizeObserver();
    this.render();
  }

  // Inject component-scoped CSS only once
  #injectCSS() {
    if (document.getElementById("bc-styles")) return;

    const css = `
      .bc-wrap {
        position: relative;
        width: 100%;
        height: 340px;
        font-family: sans-serif;
      }

      .bc-svg {
        width: 100%;
        height: 100%;
      }

      .bc-bar {        
        opacity: 0.8;
        cursor: pointer;
        transition: 0.15s;
      }
      .bc-bar:hover {
        opacity: 1;
      }

      .bc-x {
        font-size: 12px;
        fill: #666;
        text-anchor: middle;
      }

      .bc-y {
        font-size: 11px;
        fill: #999;
        text-anchor: end;
      }

      .bc-tip {
        position: absolute;
        background: rgba(0,0,0,0.85);
        color: #fff;
        padding: 5px 8px;
        border-radius: 6px;
        font-size: 12px;
        display: none;
        pointer-events: none;
        white-space: nowrap;
        transform: translate(5px, -10px);
        
      }
    `;

    const tag = document.createElement("style");
    tag.id = "bc-styles";
    tag.textContent = css;
    document.head.appendChild(tag);
  }

  #setupResizeObserver() {
    const ro = new ResizeObserver(() => this.render());
    ro.observe(this.wrap);
  }

  update(opts = {}) {
    this.#processData(opts);
    this.render();
  }

  // Helper to handle Array<Number> or Array<{label, value}>
  #processData(opts) {
    let rawData = opts.data;

    // If no data passed, and we already have data, keep it
    if (!rawData && this.data) rawData = this.data;

    // Default Fallback
    if (!rawData) {
      this.data = [10, 20, 15, 30, 25, 40, 22];
      this.dataLabels = this.data.map(String); // Default labels are just the values
      this.xAxisLabels = ["1", "2", "3", "4", "5", "6", "7"];
      return;
    }

    // Check Format
    if (rawData.length > 0 && typeof rawData[0] === "object") {
      // Object Array: [{label: '1hr', value: 10}, ...]
      this.data = rawData.map((d) => d.value);
      this.dataLabels = rawData.map((d) => d.label || String(d.value)); // Use label if present, else value
    } else {
      // Number Array: [10, 20, ...]
      this.data = rawData;
      this.dataLabels = rawData.map(String);
    }

    // Handle X-Axis Labels
    if (opts.xAxisLabels) {
      this.xAxisLabels = opts.xAxisLabels;
    } else if (
      !this.xAxisLabels ||
      this.xAxisLabels.length !== this.data.length
    ) {
      // Generate numeric if missing
      this.xAxisLabels = this.data.map((_, i) => String(i + 1));
    }
  }

  render() {
    const svg = this.svg;
    const width = svg.clientWidth;
    const height = svg.clientHeight;

    svg.setAttribute("viewBox", `0 0 ${width} ${height}`);
    svg.innerHTML = "";

    const pad = { top: 20, right: 20, bottom: 40, left: 40 };
    const w = width - pad.left - pad.right;
    const h = height - pad.top - pad.bottom;

    const g = this.#el("g", { transform: `translate(${pad.left},${pad.top})` });
    svg.appendChild(g);

    const maxVal = Math.max(...this.data) || 1; // Avoid divide by zero
    const avg = this.data.reduce((a, b) => a + b, 0) / this.data.length;

    const scaleY = (v) => (v / maxVal) * h;

    const n = this.data.length;

    // ---- FIXED BAR WIDTH & SPACING ----
    const barW = 45; // Slightly thinner to allow space
    const sidePad = 10; // Space from Y-axis and right edge
    const totalBarWidth = barW * n;
    const remaining = w - totalBarWidth - sidePad * 2;
    const gap = n > 1 ? remaining / (n - 1) : 0;

    // ----- AXIS LINES -----

    // Y Axis
    g.appendChild(
      this.#el("line", {
        x1: 0,
        y1: 0,
        x2: 0,
        y2: h,
        stroke: "#d1d5db",
        "stroke-width": 2,
      })
    );

    // X Axis
    g.appendChild(
      this.#el("line", {
        x1: 0,
        y1: h,
        x2: w,
        y2: h,
        stroke: "#d1d5db",
        "stroke-width": 2,
      })
    );

    // ----- Y TICKS -----
    for (let i = 0; i <= 4; i++) {
      const val = (maxVal / 4) * i;
      const y = h - scaleY(val);

      g.appendChild(
        this.#el(
          "text",
          {
            x: -8,
            y: y + 4,
            class: "bc-y",
          },
          Math.round(val)
        )
      );
    }

    // ----- AVERAGE LINE -----
    const avgY = h - scaleY(avg);

    // 1. Visual Line (No events)
    g.appendChild(
      this.#el("line", {
        x1: 0,
        x2: w,
        y1: avgY,
        y2: avgY,
        stroke: "#ef4444",
        "stroke-dasharray": "5 5",
        "stroke-width": 2,
        class: "bc-avg-line",
        "pointer-events": "none", // Let events pass to the hit area
      })
    );

    // 2. Invisible Hit Area (Thicker for better UX)
    const avgHitArea = this.#el("line", {
      x1: 0,
      x2: w,
      y1: avgY,
      y2: avgY,
      stroke: "transparent",
      "stroke-width": 20, // 20px tall hit zone
      style: "cursor: pointer",
    });

    avgHitArea.addEventListener("pointerenter", (e) =>
      this.#showTip(e, `Average: ${avg.toFixed(2)}`)
    );
    avgHitArea.addEventListener("pointermove", (e) => this.#moveTip(e));
    avgHitArea.addEventListener("pointerleave", () => this.#hideTip());

    g.appendChild(avgHitArea);

    // ----- BARS + LABELS -----
    this.data.forEach((val, i) => {
      const barHeight = scaleY(val);
      const x = sidePad + i * (barW + gap);
      const y = h - barHeight;
      const r = 6; // Radius

      // Path for top rounded corners
      const d = [
        `M ${x},${y + barHeight}`,
        `L ${x},${y + r}`,
        `a ${r},${r} 0 0 1 ${r},-${r}`,
        `L ${x + barW - r},${y}`,
        `a ${r},${r} 0 0 1 ${r},${r}`,
        `L ${x + barW},${y + barHeight}`,
        "Z",
      ].join(" ");

      const bar = this.#el("path", {
        d,
        fill: "#3b82f6",
        class: "bc-bar",
      });

      // Use this.dataLabels[i] for the tooltip text
      bar.addEventListener("pointerenter", (e) =>
        this.#showTip(e, `${this.dataLabels[i]}`)
      );
      bar.addEventListener("pointermove", (e) => this.#moveTip(e));
      bar.addEventListener("pointerleave", () => this.#hideTip());

      g.appendChild(bar);

      // Render X-Axis Label
      g.appendChild(
        this.#el(
          "text",
          {
            x: x + barW / 2,
            y: h + 18,
            class: "bc-x",
          },
          this.xAxisLabels[i]
        )
      );
    });
  }

  #showTip(ev, text) {
    this.tooltip.textContent = text;
    this.tooltip.style.display = "block";
    this.#moveTip(ev);
  }

  #moveTip(ev) {
    const r = this.wrap.getBoundingClientRect();
    this.tooltip.style.left = ev.clientX - r.left + "px";
    this.tooltip.style.top = ev.clientY - r.top - 10 + "px";
  }

  #hideTip() {
    this.tooltip.style.display = "none";
  }

  #el(tag, attrs = {}, text = null) {
    const el = document.createElementNS("http://www.w3.org/2000/svg", tag);
    for (const k in attrs) el.setAttribute(k, attrs[k]);
    if (text) el.textContent = text;
    return el;
  }
}
