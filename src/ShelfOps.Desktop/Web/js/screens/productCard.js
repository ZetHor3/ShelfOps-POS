// productCard.js — детальная карточка товара + остатки по складам.
import * as bridge from "../bridge.js";
import * as ui from "../ui.js";
import * as app from "../app.js";

export default {
  title: "Карточка товара",

  async render(container, params) {
    const id = params.id;
    if (!id) { container.replaceChildren(ui.errorBox("Не указан ID товара")); return; }

    container.replaceChildren(ui.spinner("Загружаем товар…"));

    let product, stock;
    try {
      [product, stock] = await Promise.all([
        bridge.get(`api/v1/products/${id}`),
        bridge.get(`api/v1/products/${id}/stock`),
      ]);
    } catch (err) {
      container.replaceChildren(ui.errorBox(err.message, () => this.render(container, params)));
      return;
    }

    ui.clear(container);

    const backBtn = ui.h("button", {
      class: "btn btn-ghost btn-sm",
      onclick: () => app.go("Products"),
      html: ui.icon("back", 14),
    }, "Назад к товарам");

    const title = ui.h("h2", {}, product.name);

    const grid = ui.h("div", { class: "detail-grid" },
      ui.h("div", { class: "detail-row" },
        ui.h("span", { class: "detail-label" }, "SKU"),
        ui.h("span", { class: "detail-value" }, product.sku)),
      ui.h("div", { class: "detail-row" },
        ui.h("span", { class: "detail-label" }, "Категория"),
        ui.h("span", { class: "detail-value" }, product.categoryName || "—")),
      ui.h("div", { class: "detail-row" },
        ui.h("span", { class: "detail-label" }, "Штрих-код"),
        ui.h("span", { class: "detail-value" }, product.barcode || "—")),
      ui.h("div", { class: "detail-row" },
        ui.h("span", { class: "detail-label" }, "Закупочная цена"),
        ui.h("span", { class: "detail-value" }, ui.money(product.purchasePrice))),
      ui.h("div", { class: "detail-row" },
        ui.h("span", { class: "detail-label" }, "Продажная цена"),
        ui.h("span", { class: "detail-value strong" }, ui.money(product.salePrice))),
      ui.h("div", { class: "detail-row" },
        ui.h("span", { class: "detail-label" }, "Статус"),
        ui.h("span", {}, ui.badgeActive(product.isActive))),
      ui.h("div", { class: "detail-row" },
        ui.h("span", { class: "detail-label" }, "Общий остаток"),
        ui.h("span", { class: "detail-value strong" }, String(product.stockTotal ?? 0))),
    );

    if (product.description) {
      grid.append(ui.h("div", { class: "detail-row" },
        ui.h("span", { class: "detail-label" }, "Описание"),
        ui.h("span", { class: "detail-value muted" }, product.description)));
    }

    container.append(ui.h("div", { class: "toolbar" }, backBtn));
    container.append(title);
    container.append(grid);

    /* Таблица остатков по складам */
    const stockItems = Array.isArray(stock) ? stock : [];
    if (stockItems.length > 0) {
      container.append(ui.h("h3", { style: "margin-top:1.5rem" }, "Остатки по складам"));

      const rows = stockItems.map((s) => ui.h("tr", {},
        ui.h("td", {}, s.warehouseName || "—"),
        ui.h("td", { class: "num strong" }, String(s.quantityOnHand ?? 0))));

      container.append(ui.h("div", { class: "table-wrap" },
        ui.h("table", { class: "tbl" },
          ui.h("thead", {}, ui.h("tr", {},
            ui.h("th", {}, "Склад"),
            ui.h("th", { class: "num" }, "Количество"))),
          ui.h("tbody", {}, rows))));
    } else {
      container.append(ui.emptyBox("Остатков по складам нет"));
    }

    /* Кнопки действий */
    if (canEdit()) {
      const actions = ui.h("div", { class: "form-actions", style: "margin-top:1.5rem" },
        ui.h("button", { class: "btn", onclick: () => app.go("Products") }, "Назад"),
        ui.h("button", { class: "btn btn-primary", onclick: () => openEditModal(product) },
          ui.h("span", { html: ui.icon("edit", 14) }), " Редактировать"));
      container.append(actions);
    }
  },
};

function canEdit() {
  const role = app.state.user?.role;
  return role === "Administrator" || role === "Manager";
}

/* Переиспользуем модалку редактирования из products.js (дублируем логику) */
async function openEditModal(product) {
  let categories = [];
  try { categories = (await bridge.get("api/v1/categories")) || []; } catch {}

  const f = {
    name: ui.h("input", { type: "text", id: "f-name", value: product?.name || "" }),
    sku: ui.h("input", { type: "text", id: "f-sku", value: product?.sku || "" }),
    desc: ui.h("textarea", { rows: 2, id: "f-desc" }, product?.description || ""),
    barcode: ui.h("input", { type: "text", id: "f-barcode", value: product?.barcode || "" }),
    category: ui.h("select", {},
      ui.h("option", { value: "" }, "Без категории"),
      categories.map((c) => ui.h("option", { value: c.id, selected: product?.categoryId === c.id ? true : null }, c.name))),
    purchase: ui.h("input", { type: "number", step: "0.01", min: "0", id: "f-purchase", value: product?.purchasePrice ?? "" }),
    sale: ui.h("input", { type: "number", step: "0.01", min: "0", id: "f-sale", value: product?.salePrice ?? "" }),
    active: ui.h("input", { type: "checkbox", id: "f-active", checked: product ? product.isActive : true }),
  };

  const { close } = ui.openModal({
    title: "Редактировать товар",
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
      ui.h("label", { class: "chk" }, f.active, ui.h("span", {}, "Товар активен (виден в продажах)"))),
    actions: [
      { label: "Отмена", cls: "btn-ghost", onClick: () => {} },
      { label: "Сохранить", cls: "btn-primary", onClick: () => saveEdit(f, product, close) },
    ],
  });
}

async function saveEdit(f, product, close) {
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
    await bridge.put(`api/v1/products/${product.id}`, body);
    close();
    ui.toast("Товар обновлён", "success");
    app.go("ProductCard", { id: product.id });
  } catch (err) {
    ui.toast(err.message, "error");
  }
}