// products.js — каталог товаров: поиск, пагинация, создание/редактирование (модалка).
import * as bridge from "../bridge.js";
import * as ui from "../ui.js";
import * as app from "../app.js";

const s = { page: 1, totalPages: 1, totalCount: 0, search: "" };

export default {
  title: "Товары",

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

let containerRef = null;

function buildPage(container, data) {
  return ui.h("div", {},
    ui.h("div", { class: "toolbar" },
      ui.h("div", { class: "search-box", html: ui.icon("search", 15) },
        ui.h("input", {
          type: "search", placeholder: "Поиск: название или SKU…",
          value: s.search,
          oninput: (e) => { s.search = e.target.value; },
          onkeydown: (e) => { if (e.key === "Enter") { s.page = 1; app.go("Products"); } },
        })),
      ui.h("button", { class: "btn", onclick: () => { s.page = 1; app.go("Products"); } }, "Найти"),
      ui.h("span", { class: "spacer" }),
      canEdit() ? ui.h("button", { class: "btn btn-primary", onclick: () => openProductModal() },
        ui.h("span", { html: ui.icon("plus", 14) }), "Добавить товар") : null),
    renderTable(data),
    renderPagination());
}

async function loadPage() {
  let url = `api/v1/products?page=${s.page}&pageSize=${ui.pageSize()}`;
  if (s.search.trim()) url += "&search=" + encodeURIComponent(s.search.trim());
  const data = await bridge.get(url);
  s.totalPages = data.totalPages ?? 1;
  s.totalCount = data.totalCount ?? 0;
  if (s.page > s.totalPages) { s.page = Math.max(1, s.totalPages); return loadPage(); }
  return data;
}

function canEdit() {
  const role = app.state.user?.role;
  return role === "Administrator" || role === "Manager";
}
function canDelete() {
  return app.state.user?.role === "Administrator";
}

function renderTable(data) {
  const items = data.items || [];
  if (!items.length) return ui.emptyBox(s.search ? "Ничего не найдено по запросу." : "Товаров пока нет");

  const rows = items.map((p) => ui.h("tr", { class: "clickable", onclick: () => app.go("ProductCard", { id: p.id }) },
    ui.h("td", { class: "muted" }, p.sku),
    ui.h("td", { class: "strong" }, p.name),
    ui.h("td", { class: "muted" }, p.categoryName || "—"),
    ui.h("td", { class: "num muted" }, p.purchasePrice != null ? ui.money(p.purchasePrice) : "—"),
    ui.h("td", { class: "num" }, ui.money(p.salePrice)),
    ui.h("td", { class: "num" }, String(p.stockTotal ?? 0)),
    ui.h("td", {}, ui.badgeActive(p.isActive)),
    ui.h("td", {}, ui.h("div", { class: "cell-actions" },
      canEdit() ? ui.h("button", { class: "btn btn-ghost btn-sm icon-btn", title: "Редактировать",
        onclick: (e) => { e.stopPropagation(); openProductModal(p); } }, ui.h("span", { html: ui.icon("edit", 14) })) : null,
      canDelete() && p.isActive ? ui.h("button", { class: "btn btn-danger-ghost btn-sm icon-btn", title: "Деактивировать",
        onclick: (e) => { e.stopPropagation(); deactivate(p); } }, ui.h("span", { html: ui.icon("trash", 14) })) : null))));

  return ui.h("div", { class: "table-wrap" },
    ui.h("table", { class: "tbl" },
      ui.h("thead", {}, ui.h("tr", {},
        ui.h("th", {}, "SKU"), ui.h("th", {}, "Название"), ui.h("th", {}, "Категория"),
        ui.h("th", { class: "num" }, "Закупка"), ui.h("th", { class: "num" }, "Продажа"),
        ui.h("th", { class: "num" }, "Остаток"), ui.h("th", {}, "Статус"), ui.h("th", {}))),
      ui.h("tbody", {}, rows)));
}

function renderPagination() {
  return ui.h("div", { class: "pagination" },
    ui.h("span", {}, `Всего: ${s.totalCount} · стр. ${s.page} из ${s.totalPages}`),
    ui.h("div", { class: "pages" },
      ui.h("button", { class: "btn btn-sm", disabled: s.page <= 1, onclick: () => { s.page--; app.go("Products"); } }, "←"),
      ui.h("button", { class: "btn btn-sm", disabled: s.page >= s.totalPages, onclick: () => { s.page++; app.go("Products"); } }, "→")));
}

/* ── форма создания/редактирования ── */
async function openProductModal(product) {
  let categories = [];
  try { categories = (await bridge.get("api/v1/categories")) || []; } catch { /* не критично */ }

  const editing = Boolean(product);
  const editingProduct = product; // keep reference for ID
  const f = {
    name: ui.h("input", { type: "text", id: "f-name", value: product?.name || "" }),
    sku: ui.h("input", { type: "text", id: "f-sku", value: product?.sku || "" }),
    desc: ui.h("textarea", { rows: 2, id: "f-desc" }, product?.description || ""),
    barcode: ui.h("input", { type: "text", id: "f-barcode", value: product?.barcode || "" }),
    category: ui.h("select", {},
      ui.h("option", { value: "" }, "Без категории"),
      categories.map((c) => ui.h("option", { value: c.id, selected: product?.categoryId === c.id ? "selected" : null }, c.name))),
    purchase: ui.h("input", { type: "number", step: "0.01", min: "0", id: "f-purchase", value: product?.purchasePrice ?? "" }),
    sale: ui.h("input", { type: "number", step: "0.01", min: "0", id: "f-sale", value: product?.salePrice ?? "" }),
    active: ui.h("input", { type: "checkbox", id: "f-active", checked: product ? product.isActive : true }),
  };

  const { close } = ui.openModal({
    title: editing ? "Редактировать товар" : "Новый товар",
    wide: true,
    body: ui.h("div", {},
      ui.h("div", { class: "form-row" },
        ui.h("div", { class: "form-field" }, ui.h("label", {}, "Название*"), f.name),
        ui.h("div", { class: "form-field" }, ui.h("label", {}, "SKU*"), f.sku)),
      ui.h("div", { class: "form-row" },
        ui.h("div", { class: "form-field" }, ui.h("label", {}, "Категория"), f.category),
        ui.h("div", { class: "form-field" }, ui.h("label", {}, "Штрих-код"), f.barcode)),
      ui.h("div", { class: "form-row" },
        ui.h("div", { class: "form-field" }, ui.h("label", {}, "Закупочная цена"), f.purchase),
        ui.h("div", { class: "form-field" }, ui.h("label", {}, "Продажная цена"), f.sale)),
      ui.h("div", { class: "form-field" }, ui.h("label", {}, "Описание"), f.desc),
      ui.h("label", { class: "chk" },
        f.active,
        ui.h("span", {}, "Товар активен (виден в продажах)"))),
    actions: [
      { label: "Отмена", cls: "btn-ghost", onClick: () => {} },
      { label: editing ? "Сохранить" : "Создать", cls: "btn-primary", onClick: () => saveProduct(f, editing, editingProduct, close) },
    ],
  });
}

async function saveProduct(f, editing, product, close) {
  const body = {
    name: f.name.value.trim(),
    sku: f.sku.value.trim(),
    description: f.desc.value.trim(),
    barcode: f.barcode.value.trim() || null,
    categoryId: f.category.value || null,
    purchasePrice: Number(f.purchase.value || 0),
    salePrice: Number(f.sale.value || 0),
    isActive: f.active.checked,
  };
  if (!body.name || !body.sku) { ui.toast("Укажите название и SKU", "error"); return; }

  try {
    if (editing) await bridge.put(`api/v1/products/${product.id}`, body);
    else await bridge.post("api/v1/products", body);
    close();
    ui.toast(editing ? "Товар обновлён" : "Товар создан", "success");
    app.go("Products");
  } catch (err) {
    ui.toast(err.message, "error");
  }
}

async function deactivate(p) {
  const ok = await ui.confirmDialog(`Деактивировать товар «${p.name}»? Он исчезнет из продаж.`);
  if (!ok) return;
  try {
    await bridge.del(`api/v1/products/${p.id}`);
    ui.toast("Товар деактивирован", "success");
    app.go("Products");
  } catch (err) {
    ui.toast(err.message, "error");
  }
}