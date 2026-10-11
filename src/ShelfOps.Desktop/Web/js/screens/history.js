// history.js — история продаж с пагинацией.
import * as bridge from "../bridge.js";
import * as ui from "../ui.js";
import * as app from "../app.js";

const s = { page: 1, totalPages: 1, totalCount: 0 };

export default {
  title: "История продаж",

  async render(container) {
    container.replaceChildren(ui.spinner());
    let data;
    try {
      data = await loadPage();
    } catch (err) {
      container.replaceChildren(ui.errorBox(err.message, () => this.render(container)));
      return;
    }
    container.replaceChildren(buildPage(container, data));
  },
};

async function loadPage() {
  const url = `api/v1/sales?page=${s.page}&pageSize=${ui.pageSize()}`;
  const data = await bridge.get(url);
  s.totalPages = data.totalPages ?? 1;
  s.totalCount = data.totalCount ?? 0;
  if (s.page > s.totalPages) { s.page = Math.max(1, s.totalPages); return loadPage(); }
  return data;
}

function buildPage(container, data) {
  const items = data.items || [];
  if (!items.length) return ui.emptyBox("История продаж пуста");

  const rows = items.map((sale) =>
    ui.h("tr", {},
      ui.h("td", { class: "strong" }, sale.saleNumber),
      ui.h("td", {}, ui.dateTime(sale.createdAtUtc)),
      ui.h("td", {}, ui.badge(sale.status)),
      ui.h("td", { class: "num strong" }, ui.money(sale.grandTotal))));

  return ui.h("div", {},
    ui.h("div", { class: "table-wrap" },
      ui.h("table", { class: "tbl" },
        ui.h("thead", {}, ui.h("tr", {},
          ui.h("th", {}, "№"),
          ui.h("th", {}, "Дата"),
          ui.h("th", {}, "Статус"),
          ui.h("th", { class: "num" }, "Сумма"))),
        ui.h("tbody", {}, rows))),
    renderPagination());
}

function renderPagination() {
  return ui.h("div", { class: "pagination" },
    ui.h("span", {}, `Всего: ${s.totalCount} · стр. ${s.page} из ${s.totalPages}`),
    ui.h("div", { class: "pages" },
      ui.h("button", {
        class: "btn btn-sm",
        disabled: s.page <= 1,
        onclick: () => { s.page--; app.go("SalesHistory"); },
      }, "←"),
      ui.h("button", {
        class: "btn btn-sm",
        disabled: s.page >= s.totalPages,
        onclick: () => { s.page++; app.go("SalesHistory"); },
      }, "→")));
}