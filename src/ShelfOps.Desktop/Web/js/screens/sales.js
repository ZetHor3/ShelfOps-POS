// sales.js — корзина продажи: поиск товара, добавление, проведение.
import * as bridge from "../bridge.js";
import * as ui from "../ui.js";
import * as app from "../app.js";

export default {
  title: "Продажи",

  async render(container) {
    container.replaceChildren(ui.spinner());

    let warehouses = [], products = [];
    try {
      [warehouses, products] = await Promise.all([
        bridge.get("api/v1/warehouses"),
        bridge.get("api/v1/products?pageSize=200&isActive=true"),
      ]);
    } catch (err) {
      container.replaceChildren(ui.errorBox(err.message, () => this.render(container)));
      return;
    }

    const state = {
      warehouseId: "",
      items: [],           // { productId, sku, name, price, quantity, discount }
      searchResults: [],
      searchQuery: "",
    };

    ui.clear(container);
    const el = buildPage(container, state, warehouses, products.items || []);
    container.append(el);
  },
};

function buildPage(container, state, warehouses, allProducts) {
  const wrap = ui.h("div", {});

  /* Верхняя панель: склад + поиск */
  const toolbar = ui.h("div", { class: "toolbar" },
    ui.h("div", { class: "form-field", style: "width:200px" },
      ui.h("label", {}, "Склад"),
      ui.h("select", {
        onchange: (e) => { state.warehouseId = e.target.value; updateCompleteBtn(); },
      },
        ui.h("option", { value: "" }, "Выберите склад"),
        warehouses.filter((w) => w.isActive).map((w) =>
          ui.h("option", { value: w.id, selected: state.warehouseId === w.id }, w.name)))));

  const searchBox = ui.h("div", { class: "search-box", style: "flex:1;max-width:400px", html: ui.icon("search", 15) },
    ui.h("input", {
      type: "search", placeholder: "Поиск товара по названию или SKU…",
      value: state.searchQuery,
      oninput: (e) => {
        state.searchQuery = e.target.value;
        renderSearchResults(state, allProducts, resultsEl);
      },
      onkeydown: (e) => {
        if (e.key === "Enter" && state.searchResults.length > 0) {
          addToCart(state, state.searchResults[0]);
          state.searchQuery = "";
          renderSearchResults(state, allProducts, resultsEl);
          e.target.value = "";
          renderCart(state, cartEl, totalEl);
        }
      },
    }));

  toolbar.append(searchBox);
  wrap.append(toolbar);

  const resultsEl = ui.h("div", { class: "search-results" });
  wrap.append(resultsEl);

  /* Таблица корзины */
  const cartEl = ui.h("div", {});
  const totalEl = ui.h("div", { class: "cart-total" });
  wrap.append(cartEl);
  wrap.append(totalEl);

  renderCart(state, cartEl, totalEl);

  /* Кнопка проведения */
  const completeBtn = ui.h("button", {
    class: "btn btn-primary btn-block",
    style: "margin-top:1rem",
    disabled: true,
    onclick: () => completeSale(state, warehouses),
  }, "Провести продажу");
  wrap.append(completeBtn);

  return wrap;
}

function renderSearchResults(state, allProducts, el) {
  const q = state.searchQuery.trim().toLowerCase();
  ui.clear(el);
  if (!q || q.length < 1) return;

  state.searchResults = allProducts.filter((p) =>
    p.isActive && (
      p.name.toLowerCase().includes(q) ||
      (p.sku && p.sku.toLowerCase().includes(q)) ||
      (p.barcode && p.barcode.toLowerCase().includes(q))
    )
  ).slice(0, 10);

  if (!state.searchResults.length) return;

  for (const p of state.searchResults) {
    const alreadyInCart = state.items.some((i) => i.productId === p.id);
    el.append(ui.h("div", {
      class: "search-result-item" + (alreadyInCart ? " dim" : ""),
      onclick: () => {
        if (alreadyInCart) return;
        addToCart(state, p);
        state.searchQuery = "";
        renderSearchResults(state, allProducts, el);
        document.querySelector(".search-box input").value = "";
      },
    },
      ui.h("span", { class: "strong" }, p.name),
      ui.h("span", { class: "muted" }, p.sku),
      ui.h("span", { class: "num" }, ui.money(p.salePrice))));
  }
}

function addToCart(state, product) {
  state.items.push({
    productId: product.id,
    sku: product.sku,
    name: product.name,
    price: product.salePrice,
    quantity: 1,
    discount: 0,
  });
  // re-render cart — find elements by class
  const cartEl = document.querySelector(".cart-table-wrap");
  const totalEl = document.querySelector(".cart-total");
  if (cartEl) renderCart(state, cartEl.parentElement, totalEl);
}

function renderCart(state, container, totalEl) {
  if (!state.items.length) {
    ui.clear(container);
    container.append(ui.emptyBox("Корзина пуста. Начните поиск товара.", "🛒"));
    ui.clear(totalEl);
    return;
  }

  const rows = state.items.map((item, idx) => {
    const subtotal = item.price * item.quantity - item.discount;
    return ui.h("tr", {},
      ui.h("td", { class: "strong" }, item.name),
      ui.h("td", { class: "muted" }, item.sku),
      ui.h("td", { class: "num" }, ui.money(item.price)),
      ui.h("td", {},
        ui.h("div", { class: "qty-control" },
          ui.h("button", { class: "btn btn-sm btn-ghost", onclick: () => { changeQty(state, idx, -1); renderCart(state, container, totalEl); } }, "−"),
          ui.h("span", {}, String(item.quantity)),
          ui.h("button", { class: "btn btn-sm btn-ghost", onclick: () => { changeQty(state, idx, 1); renderCart(state, container, totalEl); } }, "+"))),
      ui.h("td", {},
        ui.h("input", {
          type: "number", step: "0.01", min: "0",
          class: "inp-sm",
          value: item.discount || "",
          placeholder: "0",
          onchange: (e) => { item.discount = Number(e.target.value) || 0; renderCart(state, container, totalEl); },
        })),
      ui.h("td", { class: "num strong" }, ui.money(subtotal)),
      ui.h("td", {},
        ui.h("button", {
          class: "btn btn-danger-ghost btn-sm icon-btn",
          onclick: () => { state.items.splice(idx, 1); renderCart(state, container, totalEl); },
          title: "Удалить",
        }, ui.h("span", { html: ui.icon("trash", 14) }))));
  });

  const grandTotal = state.items.reduce((sum, item) => sum + item.price * item.quantity - item.discount, 0);

  ui.clear(container);
  container.append(ui.h("div", { class: "table-wrap cart-table-wrap" },
    ui.h("table", { class: "tbl" },
      ui.h("thead", {}, ui.h("tr", {},
        ui.h("th", {}, "Товар"),
        ui.h("th", {}, "SKU"),
        ui.h("th", { class: "num" }, "Цена"),
        ui.h("th", {}, "Кол-во"),
        ui.h("th", {}, "Скидка"),
        ui.h("th", { class: "num" }, "Сумма"),
        ui.h("th", {}))),
      ui.h("tbody", {}, rows))));

  ui.clear(totalEl);
  totalEl.append(ui.h("div", { class: "total-bar" },
    ui.h("span", { class: "total-label" }, "Итого:"),
    ui.h("span", { class: "total-value" }, ui.money(grandTotal))));

  // Update complete button
  const completeBtn = container.parentElement.querySelector(".btn-block");
  if (completeBtn) completeBtn.disabled = state.items.length === 0 || !state.warehouseId;
}

function changeQty(state, idx, delta) {
  const item = state.items[idx];
  item.quantity = Math.max(1, item.quantity + delta);
}

function updateCompleteBtn() {
  const btn = document.querySelector(".btn-block");
  if (btn) {
    // find the nearest cart state via DOM or just check rendered cart
    const container = btn.closest("div");
    const rows = container?.querySelector("tbody")?.children.length || 0;
    btn.disabled = rows === 0;
  }
}

async function completeSale(state, warehouses) {
  if (!state.warehouseId) { ui.toast("Выберите склад", "error"); return; }
  if (!state.items.length) { ui.toast("Добавьте товары в корзину", "error"); return; }

  const idempotencyKey = crypto.randomUUID?.() || Date.now().toString(36) + Math.random().toString(36).slice(2);

  const body = {
    warehouseId: state.warehouseId,
    idempotencyKey,
    items: state.items.map((item) => ({
      productId: item.productId,
      quantity: item.quantity,
      discountAmount: item.discount,
    })),
  };

  try {
    const result = await bridge.post("api/v1/sales/complete", body);
    ui.toast(`Продажа ${result.saleNumber || ""} проведена на ${ui.money(result.grandTotal)}`, "success");
    state.items = [];
    state.warehouseId = "";
    state.searchQuery = "";
    app.go("Sales");
  } catch (err) {
    ui.toast(err.message, "error");
  }
}