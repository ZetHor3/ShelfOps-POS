// receipts.js — форма поступления: склад + строки товаров.
import * as bridge from "../bridge.js";
import * as ui from "../ui.js";
import * as app from "../app.js";

export default {
  title: "Поступления",

  async render(container) {
    container.replaceChildren(ui.spinner());

    if (!canManage()) {
      container.replaceChildren(ui.errorBox("Доступ только у администраторов и менеджеров."));
      return;
    }

    let warehouses = [], products = [];
    try {
      [warehouses, products] = await Promise.all([
        bridge.get("api/v1/warehouses"),
        bridge.get("api/v1/products?pageSize=200"),
      ]);
    } catch (err) {
      container.replaceChildren(ui.errorBox(err.message, () => this.render(container)));
      return;
    }

    const state = {
      warehouseId: "",
      lines: [],      // { productId, sku, name, quantity, unitCost }
      searchQuery: "",
      searchResults: [],
    };

    ui.clear(container);
    container.append(buildPage(state, warehouses, products.items || []));
  },
};

function canManage() {
  const role = app.state.user?.role;
  return role === "Administrator" || role === "Manager";
}

function buildPage(state, warehouses, allProducts) {
  const wrap = ui.h("div", {});

  /* Склад */
  const toolbar = ui.h("div", { class: "toolbar" },
    ui.h("div", { class: "form-field", style: "width:240px" },
      ui.h("label", {}, "Склад*"),
      ui.h("select", {
        id: "receipt-warehouse",
        onchange: (e) => { state.warehouseId = e.target.value; },
      },
        ui.h("option", { value: "" }, "Выберите склад"),
        warehouses.filter((w) => w.isActive).map((w) =>
          ui.h("option", { value: w.id }, w.name)))));

  /* Поиск товара для добавления */
  const searchBox = ui.h("div", { class: "search-box", style: "flex:1;max-width:400px", html: ui.icon("search", 15) },
    ui.h("input", {
      type: "search", placeholder: "Поиск товара…",
      oninput: (e) => {
        state.searchQuery = e.target.value;
        renderSearch(state, allProducts, resultsEl);
      },
      onkeydown: (e) => {
        if (e.key === "Enter" && state.searchResults.length > 0) {
          addLine(state, state.searchResults[0]);
          state.searchQuery = "";
          e.target.value = "";
          renderSearch(state, allProducts, resultsEl);
          renderLines(state, linesEl, totalEl);
        }
      },
    }));
  toolbar.append(searchBox);
  wrap.append(toolbar);

  const resultsEl = ui.h("div", { class: "search-results" });
  wrap.append(resultsEl);

  /* Таблица строк */
  const linesEl = ui.h("div", {});
  const totalEl = ui.h("div", {});
  wrap.append(linesEl);
  wrap.append(totalEl);
  renderLines(state, linesEl, totalEl);

  /* Кнопка сохранения */
  wrap.append(ui.h("button", {
    class: "btn btn-primary btn-block",
    style: "margin-top:1rem",
    onclick: () => saveReceipt(state),
  }, "Сохранить поступление"));

  return wrap;
}

function renderSearch(state, allProducts, el) {
  const q = state.searchQuery.trim().toLowerCase();
  ui.clear(el);
  if (!q || q.length < 1) return;

  state.searchResults = allProducts.filter((p) =>
    p.name.toLowerCase().includes(q) || (p.sku && p.sku.toLowerCase().includes(q))
  ).slice(0, 10);

  if (!state.searchResults.length) return;

  for (const p of state.searchResults) {
    const alreadyAdded = state.lines.some((l) => l.productId === p.id);
    el.append(ui.h("div", { class: "search-result-item" + (alreadyAdded ? " dim" : ""),
      onclick: () => {
        if (alreadyAdded) return;
        addLine(state, p);
        state.searchQuery = "";
        renderSearch(state, allProducts, el);
        const inp = document.querySelector(".search-box input");
        if (inp) inp.value = "";
      },
    },
      ui.h("span", { class: "strong" }, p.name),
      ui.h("span", { class: "muted" }, p.sku)));
  }
}

function addLine(state, product) {
  state.lines.push({
    productId: product.id,
    sku: product.sku,
    name: product.name,
    quantity: 1,
    unitCost: product.purchasePrice || 0,
  });
}

function renderLines(state, container, totalEl) {
  if (!state.lines.length) {
    ui.clear(container);
    container.append(ui.emptyBox("Добавьте товары в поступление", "📦"));
    ui.clear(totalEl);
    return;
  }

  const rows = state.lines.map((line, idx) => {
    const lineTotal = line.quantity * line.unitCost;
    return ui.h("tr", {},
      ui.h("td", { class: "strong" }, line.name),
      ui.h("td", { class: "muted" }, line.sku),
      ui.h("td", {},
        ui.h("input", {
          type: "number", min: "1", step: "1",
          class: "inp-sm",
          value: line.quantity,
          onchange: (e) => { line.quantity = Math.max(1, Number(e.target.value) || 1); renderLines(state, container, totalEl); },
        })),
      ui.h("td", {},
        ui.h("input", {
          type: "number", min: "0", step: "0.01",
          class: "inp-sm",
          value: line.unitCost || "",
          placeholder: "0",
          onchange: (e) => { line.unitCost = Number(e.target.value) || 0; renderLines(state, container, totalEl); },
        })),
      ui.h("td", { class: "num strong" }, ui.money(lineTotal)),
      ui.h("td", {},
        ui.h("button", {
          class: "btn btn-danger-ghost btn-sm icon-btn",
          title: "Удалить",
          onclick: () => { state.lines.splice(idx, 1); renderLines(state, container, totalEl); },
        }, ui.h("span", { html: ui.icon("trash", 14) }))));
  });

  const grandTotal = state.lines.reduce((s, l) => s + l.quantity * l.unitCost, 0);

  ui.clear(container);
  container.append(ui.h("div", { class: "table-wrap" },
    ui.h("table", { class: "tbl" },
      ui.h("thead", {}, ui.h("tr", {},
        ui.h("th", {}, "Товар"),
        ui.h("th", {}, "SKU"),
        ui.h("th", {}, "Кол-во"),
        ui.h("th", {}, "Цена"),
        ui.h("th", { class: "num" }, "Сумма"),
        ui.h("th", {}))),
      ui.h("tbody", {}, rows))));

  ui.clear(totalEl);
  totalEl.append(ui.h("div", { class: "total-bar" },
    ui.h("span", { class: "total-label" }, "Итого:"),
    ui.h("span", { class: "total-value" }, ui.money(grandTotal))));
}

async function saveReceipt(state) {
  if (!state.warehouseId) { ui.toast("Выберите склад", "error"); return; }
  if (!state.lines.length) { ui.toast("Добавьте хотя бы один товар", "error"); return; }

  const body = {
    warehouseId: state.warehouseId,
    items: state.lines.map((l) => ({
      productId: l.productId,
      quantity: l.quantity,
      unitCost: l.unitCost,
    })),
  };

  try {
    await bridge.post("api/v1/receipts", body);
    ui.toast("Поступление сохранено", "success");
    app.go("Receipts");
  } catch (err) {
    ui.toast(err.message, "error");
  }
}