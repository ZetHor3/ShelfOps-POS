// returns.js — список возвратов + форма создания для Admin/Manager.
import * as bridge from "../bridge.js";
import * as ui from "../ui.js";
import * as app from "../app.js";

const s = { page: 1, totalPages: 1, totalCount: 0 };

export default {
  title: "Возвраты",

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
  let url = `api/v1/returns?page=${s.page}&pageSize=${ui.pageSize()}`;
  const data = await bridge.get(url);
  s.totalPages = data.totalPages ?? 1;
  s.totalCount = data.totalCount ?? 0;
  if (s.page > s.totalPages) { s.page = Math.max(1, s.totalPages); return loadPage(); }
  return data;
}

function canCreate() {
  const role = app.state.user?.role;
  return role === "Administrator" || role === "Manager";
}

function buildPage(container, data) {
  const items = data.items || [];
  const wrap = ui.h("div", {});

  if (canCreate()) {
    wrap.append(ui.h("div", { class: "toolbar" },
      ui.h("button", { class: "btn btn-primary", onclick: () => openReturnModal() },
        ui.h("span", { html: ui.icon("plus", 14) }), " Создать возврат")));
  }

  if (!items.length) {
    wrap.append(ui.emptyBox("Возвратов пока нет"));
    return wrap;
  }

  const rows = items.map((r) =>
    ui.h("tr", {},
      ui.h("td", { class: "strong" }, r.returnNumber),
      ui.h("td", {}, r.originalSaleId ? `№${r.originalSaleId}` : "—"),
      ui.h("td", { class: "muted" }, r.reason || "—"),
      ui.h("td", {}, ui.badge(r.status)),
      ui.h("td", {}, ui.dateTime(r.createdAtUtc)),
      ui.h("td", { class: "num strong" }, ui.money(r.totalAmount))));

  wrap.append(ui.h("div", { class: "table-wrap" },
    ui.h("table", { class: "tbl" },
      ui.h("thead", {}, ui.h("tr", {},
        ui.h("th", {}, "№ возврата"),
        ui.h("th", {}, "Продажа"),
        ui.h("th", {}, "Причина"),
        ui.h("th", {}, "Статус"),
        ui.h("th", {}, "Дата"),
        ui.h("th", { class: "num" }, "Сумма"))),
      ui.h("tbody", {}, rows))));

  wrap.append(renderPagination());
  return wrap;
}

function renderPagination() {
  return ui.h("div", { class: "pagination" },
    ui.h("span", {}, `Всего: ${s.totalCount} · стр. ${s.page} из ${s.totalPages}`),
    ui.h("div", { class: "pages" },
      ui.h("button", {
        class: "btn btn-sm",
        disabled: s.page <= 1,
        onclick: () => { s.page--; app.go("Returns"); },
      }, "←"),
      ui.h("button", {
        class: "btn btn-sm",
        disabled: s.page >= s.totalPages,
        onclick: () => { s.page++; app.go("Returns"); },
      }, "→")));
}

/* ── Модалка создания возврата ── */
async function openReturnModal() {
  // Ищем продажи для выбора (последние 50)
  let sales = [];
  try {
    const data = await bridge.get("api/v1/sales?pageSize=50");
    sales = data.items || [];
  } catch { /* не критично */ }

  const f = {
    sale: ui.h("select", { id: "f-sale" },
      ui.h("option", { value: "" }, "Выберите продажу"),
      sales.map((s) =>
        ui.h("option", { value: s.id }, `${s.saleNumber} — ${ui.dateTime(s.createdAtUtc)} — ${ui.money(s.grandTotal)}`))),
    reason: ui.h("textarea", { rows: 2, id: "f-reason", placeholder: "Причина возврата…" }),
  };

  const { close } = ui.openModal({
    title: "Новый возврат",
    wide: true,
    body: ui.h("div", {},
      ui.h("div", { class: "form-field" }, ui.h("label", {}, "Продажа*"), f.sale),
      ui.h("div", { class: "form-field" }, ui.h("label", {}, "Причина"), f.reason),
      ui.h("p", { class: "muted", style: "font-size:0.85rem;margin-top:0.5rem" },
        "Возврат оформляется на все товары выбранной продажи. Для частичного возврата обратитесь к администратору.")),
    actions: [
      { label: "Отмена", cls: "btn-ghost", onClick: () => {} },
      { label: "Создать возврат", cls: "btn-primary", onClick: () => saveReturn(f, close) },
    ],
  });
}

async function saveReturn(f, close) {
  const saleId = f.sale.value;
  const reason = f.reason.value.trim();

  if (!saleId) { ui.toast("Выберите продажу", "error"); return; }

  // Отправляем без items (API оформит возврат на все позиции)
  const body = {
    originalSaleId: saleId,
    reason: reason || null,
    items: [],
  };

  try {
    await bridge.post("api/v1/returns", body);
    close();
    ui.toast("Возврат создан", "success");
    app.go("Returns");
  } catch (err) {
    ui.toast(err.message, "error");
  }
}