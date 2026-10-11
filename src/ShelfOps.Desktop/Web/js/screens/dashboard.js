// dashboard.js — KPI-карточки + SVG-график выручки + последние продажи.
import * as bridge from "../bridge.js";
import * as ui from "../ui.js";

export default {
  title: "Дашборд",

  async render(container) {
    container.replaceChildren(ui.spinner("Загружаем показатели…"));
    let data;
    try {
      data = await bridge.get("api/v1/reports/dashboard");
    } catch (err) {
      container.replaceChildren(ui.errorBox(err.message, () => this.render(container)));
      return;
    }

    ui.clear(container);

    const stats = [
      { label: "Выручка сегодня",           value: ui.money(data.todayRevenue),     tone: "green" },
      { label: "Выручка за период",         value: ui.money(data.periodRevenue),    tone: "amber" },
      { label: "Продаж",                    value: String(data.salesCount ?? 0),    tone: "blue" },
      { label: "Средний чек",               value: ui.money(data.averageReceipt),   tone: "teal" },
      { label: "Активных товаров",          value: String(data.activeProducts ?? 0), tone: "orange" },
      { label: "Стоимость склада",          value: ui.money(data.stockValue),       tone: "orange" },
      { label: "Низкий остаток",            value: String(data.lowStockCount ?? 0), tone: "red" },
    ];

    const grid = ui.h("div", { class: "stats-grid" });
    for (const s of stats) {
      grid.append(ui.h("div", { class: "card stat-card tone-" + s.tone },
        ui.h("div", { class: "stat-label" }, s.label),
        ui.h("div", { class: "stat-value" + (String(s.value).length > 9 ? " sub" : "") }, s.value)));
    }
    container.append(grid);

    const panels = ui.h("div", { class: "panel-grid" },
      ui.h("div", { class: "card card-pad" },
        ui.h("div", { class: "section-title" }, "Выручка"),
        renderChart(data.revenueDynamics || [])),
      ui.h("div", { class: "card card-pad" },
        ui.h("div", { class: "section-title" }, "Последние продажи"),
        renderRecentSales(data.recentSales || [])));
    container.append(panels);
  },
};

/* SVG-столбчатая диаграмма (без внешних библиотек, офлайн) */
function renderChart(values) {
  if (!values.length) return ui.h("div", { class: "page-empty" }, ui.h("div", {}, "Нет данных"));

  const W = 520, H = 205, PAD = 26;
  const max = Math.max(...values.map(Math.abs), 1);
  const n = values.length;
  const bw = (W - PAD * 2) / n;
  const barW = Math.min(36, bw * 0.6);

  const dayNames = ["Вс", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб"];
  const startIdx = Math.max(0, 7 - n);
  const labels = values.map((_, i) => dayNames[(startIdx + i) % 7]);

  const groups = values.map((v, i) => {
    const bh = Math.max(2, (Math.abs(v) / max) * (H - PAD * 2 - 20));
    const x = PAD + i * bw + (bw - barW) / 2 + (i === 0 ? barW / 2 : 0);
    const y = H - PAD - 2 - bh;
    return [
      ui.h("rect", { class: "bar", x, y, width: barW, height: bh, rx: 2 }),
      v ? ui.h("text", { class: "value-label", x: x + barW / 2, y: y - 5, "text-anchor": "middle" }, shortNum(v)) : null,
      ui.h("text", { class: "axis-label", x: x + barW / 2, y: H - PAD + 14, "text-anchor": "middle" }, labels[i]),
    ];
  });

  for (let gy = PAD + 20; gy < H - PAD; gy += (H - PAD * 2 - 20) / 4) {
    groups.push(ui.h("line", { class: "gridline", x1: PAD, y1: gy, x2: W - PAD, y2: gy }));
  }

  return ui.h("svg", { class: "chart", viewBox: `0 0 ${W} ${H}` }, ...groups.flat());
}

function shortNum(v) {
  const n = Number(v);
  if (n >= 1e3 && n < 1e6) return (n / 1e3).toFixed(1).replace(/\.0$/, "") + "k";
  if (n >= 1e6) return (n / 1e6).toFixed(1).replace(/\.0$/, "") + "M";
  return String(Math.round(n));
}

function renderRecentSales(sales) {
  if (!sales.length) return ui.h("div", { class: "page-empty" }, ui.h("div", {}, "Продаж пока нет"));

  const table = ui.h("table", { class: "tbl" },
    ui.h("thead", {}, ui.h("tr", {},
      ui.h("th", {}, "№"), ui.h("th", {}, "Дата"), ui.h("th", {}, "Склад"), ui.h("th", {}, "Статус"),
      ui.h("th", { class: "num" }, "Сумма"))),
    ui.h("tbody", {}, sales.slice(0, 6).map((s) => ui.h("tr", {},
      ui.h("td", { class: "strong" }, s.saleNumber),
      ui.h("td", {}, ui.dateTime(s.createdAtUtc)),
      ui.h("td", { class: "muted" }, s.warehouseName || "—"),
      ui.h("td", {}, ui.badge(s.status)),
      ui.h("td", { class: "num strong" }, ui.money(s.grandTotal))))));

  return ui.h("div", { class: "table-wrap" }, table);
}