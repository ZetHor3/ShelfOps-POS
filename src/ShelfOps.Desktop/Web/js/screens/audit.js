// audit.js — журнал аудита (только Admin).
import * as bridge from "../bridge.js";
import * as ui from "../ui.js";
import * as app from "../app.js";

const s = { page: 1, totalPages: 1, totalCount: 0 };

export default {
  title: "Журнал аудита",

  async render(container) {
    container.replaceChildren(ui.spinner());

    if (app.state.user?.role !== "Administrator") {
      container.replaceChildren(ui.errorBox("Доступ только у администраторов."));
      return;
    }

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
  const url = `api/v1/audit-logs?page=${s.page}&pageSize=${ui.pageSize()}`;
  const data = await bridge.get(url);
  s.totalPages = data.totalPages ?? 1;
  s.totalCount = data.totalCount ?? 0;
  if (s.page > s.totalPages) { s.page = Math.max(1, s.totalPages); return loadPage(); }
  return data;
}

function buildPage(container, data) {
  const items = data.items || [];

  if (!items.length) return ui.emptyBox("Записей аудита пока нет");

  const rows = items.map((log) =>
    ui.h("tr", {},
      ui.h("td", { class: "muted" }, String(log.id)),
      ui.h("td", {}, ui.dateTime(log.occurredAtUtc)),
      ui.h("td", {}, log.actorUserId ? `#${log.actorUserId}` : "—"),
      ui.h("td", {}, ui.h("span", { class: "badge " + actionTone(log.action) }, log.action)),
      ui.h("td", { class: "muted" }, log.entityType || "—"),
      ui.h("td", { class: "muted" }, log.entityId ? `#${log.entityId}` : "—"),
      ui.h("td", { class: "muted small" }, log.correlationId ? log.correlationId.slice(0, 8) + "…" : "—")));

  return ui.h("div", {},
    ui.h("div", { class: "table-wrap" },
      ui.h("table", { class: "tbl" },
        ui.h("thead", {}, ui.h("tr", {},
          ui.h("th", {}, "ID"),
          ui.h("th", {}, "Дата"),
          ui.h("th", {}, "Пользователь"),
          ui.h("th", {}, "Действие"),
          ui.h("th", {}, "Объект"),
          ui.h("th", {}, "ID объекта"),
          ui.h("th", {}, "Корреляция"))),
        ui.h("tbody", {}, rows))),
    renderPagination());
}

function actionTone(action) {
  const a = (action || "").toLowerCase();
  if (a.includes("delete") || a.includes("deactivat") || a.includes("fail")) return "danger";
  if (a.includes("create") || a.includes("login")) return "success";
  if (a.includes("update") || a.includes("edit") || a.includes("change")) return "warning";
  return "info";
}

function renderPagination() {
  return ui.h("div", { class: "pagination" },
    ui.h("span", {}, `Всего: ${s.totalCount} · стр. ${s.page} из ${s.totalPages}`),
    ui.h("div", { class: "pages" },
      ui.h("button", {
        class: "btn btn-sm",
        disabled: s.page <= 1,
        onclick: () => { s.page--; app.go("AuditLog"); },
      }, "←"),
      ui.h("button", {
        class: "btn btn-sm",
        disabled: s.page >= s.totalPages,
        onclick: () => { s.page++; app.go("AuditLog"); },
      }, "→")));
}