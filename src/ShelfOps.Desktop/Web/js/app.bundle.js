// ============================================================
// app.bundle.js — ShelfOps Desktop UI (bundled, no ES modules)
// ============================================================

// ------------------------------ bridge.js ------------------------------
const pending = new Map();
let seq = 0;
let sessionExpiredHandler = null;

function send(msg) {
  return new Promise((resolve, reject) => {
    const id = ++seq;
    msg.id = id;
    pending.set(id, { resolve, reject });
    window.chrome.webview.postMessage(JSON.stringify(msg));
  });
}

function bridgeApi(method, path, body) {
  return send({ type: "api", method, path, body: body ?? null });
}
const bridgeGet = (path) => bridgeApi("GET", path);
const bridgePost = (path, body) => bridgeApi("POST", path, body);
const bridgePut = (path, body) => bridgeApi("PUT", path, body);
const bridgeDel = (path) => bridgeApi("DELETE", path);

function bridgeWin(action) { return send({ type: "window", action }); }

function bridgeLogout() { return send({ type: "logout" }); }

function bridgeOnSessionExpired(fn) { sessionExpiredHandler = fn; }

window.chrome.webview.addEventListener("message", (e) => {
  let msg;
  try { msg = JSON.parse(e.data); } catch { return; }

  if (msg.type === "api") {
    const p = pending.get(msg.id);
    if (!p) return;
    pending.delete(msg.id);
    if (msg.ok) p.resolve(msg.data);
    else {
      const err = new Error(msg.error || `HTTP ${msg.status}`);
      err.status = msg.status;
      p.reject(err);
    }
  } else if (msg.type === "auth" && msg.status === "session-expired") {
    sessionExpiredHandler?.();
  }
});

const bridge = { api: bridgeApi, get: bridgeGet, post: bridgePost, put: bridgePut, del: bridgeDel, win: bridgeWin, logout: bridgeLogout, onSessionExpired: bridgeOnSessionExpired };

// ------------------------------ ui.js ------------------------------
const SVG_TAGS = new Set([
  "svg", "path", "rect", "circle", "line", "polyline", "polygon", "g", "text", "tspan",
  "defs", "stop", "use", "ellipse", "mask", "clippath", "pattern", "lineargradient", "radialgradient",
]);

function uiH(tag, attrs, ...children) {
  if (!attrs || typeof attrs !== "object" || Array.isArray(attrs)) {
    children = attrs ? [attrs, ...children] : children;
    attrs = {};
  }
  const isSvg = SVG_TAGS.has(tag.toLowerCase());
  const el = isSvg
    ? document.createElementNS("http://www.w3.org/2000/svg", tag)
    : document.createElement(tag);
  for (const [k, v] of Object.entries(attrs)) {
    if (v === null || v === undefined) continue;
    if (k === "class") { if (isSvg) el.setAttribute("class", v); else el.className = v; }
    else if (k === "html") el.innerHTML = v;
    else if (k === "style") el.style.cssText = v;
    else if (k.startsWith("on") && typeof v === "function") el.addEventListener(k.slice(2), v);
    else if (k === "value") el.value = v;
    else if (k === "checked") el.checked = v;
    else if (v === true) el.setAttribute(k, "");
    else el.setAttribute(k, v);
  }
  uiAppend(el, children);
  return el;
}

function uiAppend(el, children) {
  for (const c of children.flat(Infinity)) {
    if (c === null || c === undefined || c === false) continue;
    el.append(c instanceof Node ? c : document.createTextNode(String(c)));
  }
  return el;
}

const uiClear = (el) => { el.replaceChildren(); return el; };

function uiEsc(s) {
  return String(s ?? "").replace(/[&<>"']/g, (m) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[m]));
}

function uiMoney(v) {
  const n = Number(v ?? 0);
  return n.toLocaleString("ru-RU", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + " ₽";
}
function uiDate(d) {
  if (!d) return "—";
  const s = new Date(d);
  if (isNaN(s)) return String(d);
  return s.toLocaleDateString("ru-RU");
}
function uiDateTime(d) {
  if (!d) return "—";
  const s = new Date(d);
  if (isNaN(s)) return String(d);
  return s.toLocaleString("ru-RU", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" });
}

function uiSpinner(text) {
  return uiH("div", { class: "page-loading" }, uiH("div", { class: "spinner" }), uiH("div", {}, text || "Загрузка…"));
}

function uiErrorBox(message, retry) {
  return uiH("div", { class: "page-error" },
    uiH("div", { class: "big" }, "⚠"),
    uiH("div", { style: "max-width:480px" }, message),
    retry ? uiH("button", { class: "btn", onclick: retry }, "Повторить") : null);
}

function uiEmptyBox(text, big) {
  return uiH("div", { class: "page-empty" },
    uiH("div", { class: "big" }, big || "📦"),
    uiH("div", {}, text || ""));
}

const STATUS_TONE = {
  completed: "success", active: "success", activated: "success", успешно: "success",
  cancelled: "danger", blocked: "danger", заблокирован: "danger", failed: "danger",
  pending: "warning", processing: "warning", неактивен: "muted", inactive: "muted",
  returned: "info", void: "muted",
};

function uiBadge(text) {
  const key = String(text ?? "").toLowerCase();
  const tone = STATUS_TONE[key] || "muted";
  return uiH("span", { class: "badge " + tone }, uiEsc(text));
}

function uiBadgeActive(isActive) {
  return uiBadge(isActive ? "Активен" : "Неактивен");
}

function uiToast(message, type, ms) {
  type = type || "info";
  ms = ms || 3200;
  const root = document.getElementById("toast-root");
  const el = uiH("div", { class: "toast " + type }, message);
  root.append(el);
  setTimeout(() => {
    el.style.transition = "opacity .3s ease, transform .3s ease";
    el.style.opacity = "0";
    el.style.transform = "translateX(16px)";
    setTimeout(() => el.remove(), 320);
  }, ms);
}

function uiOpenModal(opts) {
  const root = document.getElementById("modal-root");
  const modal = uiH("div", { class: "modal" + (opts.wide ? " wide" : "") },
    uiH("h3", {}, opts.title),
    opts.body);

  const footer = uiH("div", { class: "form-actions" });
  for (const a of (opts.actions || [])) {
    footer.append(uiH("button", { class: "btn " + (a.cls || ""), onclick: () => { const r = a.onClick?.(modal); if (a.close !== false) close(); return r; } }, a.label));
  }
  modal.append(footer);

  const overlay = uiH("div", { class: "modal-overlay", onclick: (e) => { if (e.target === overlay) close(); } }, modal);
  root.append(overlay);

  function close() {
    overlay.remove();
    opts.onClose?.();
  }

  return { el: modal, close };
}

function uiConfirmDialog(message, title) {
  return new Promise((resolve) => {
    uiOpenModal({
      title: title || "Подтверждение",
      body: uiH("p", { style: "color:var(--text-secondary)" }, message),
      actions: [
        { label: "Отмена", cls: "btn-ghost", onClick: () => resolve(false) },
        { label: "Удалить", cls: "btn-danger", onClick: () => resolve(true) },
      ],
    });
  });
}

function uiChip(label, active, onclick) {
  return uiH("button", { class: "chip" + (active ? " on" : ""), onclick }, label);
}

const iconPaths = {
  back: '<path d="M19 12H5"/><polyline points="12 19 5 12 12 5"/>',
  search: '<circle cx="11" cy="11" r="7"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>',
  plus: '<line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>',
  minus: '<line x1="5" y1="12" x2="19" y2="12"/>',
  trash: '<polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/>',
  edit: '<path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>',
  info: '<circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/>',
  close: '<line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>',
};
function uiIcon(name, size) {
  size = size || 16;
  return `<svg viewBox="0 0 24 24" width="${size}" height="${size}" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">${iconPaths[name] || ""}</svg>`;
}

const navPaths = {
  dashboard: '<rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/>',
  products: '<path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/><polyline points="3.27 6.96 12 12.01 20.73 6.96"/><line x1="12" y1="22.08" x2="12" y2="12"/>',
  sales: '<circle cx="9" cy="21" r="1"/><circle cx="20" cy="21" r="1"/><path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"/>',
  history: '<circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>',
  returns: '<polyline points="1 4 1 10 7 10"/><path d="M3.51 15a9 9 0 1 0 2.13-9.36L1 10"/>',
  receipts: '<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/>',
  users: '<path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/>',
  audit: '<line x1="8" y1="6" x2="21" y2="6"/><line x1="8" y1="12" x2="21" y2="12"/><line x1="8" y1="18" x2="21" y2="18"/><line x1="3.5" y1="6" x2="3.51" y2="6"/><line x1="3.5" y1="12" x2="3.51" y2="12"/><line x1="3.5" y1="18" x2="3.51" y2="18"/>',
  settings: '<circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"/>',
};
function uiNavIcon(name, size) {
  size = size || 17;
  return `<svg viewBox="0 0 24 24" width="${size}" height="${size}" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">${navPaths[name] || ""}</svg>`;
}

const ui = { h: uiH, append: uiAppend, clear: uiClear, esc: uiEsc, money: uiMoney, date: uiDate, dateTime: uiDateTime, spinner: uiSpinner, errorBox: uiErrorBox, emptyBox: uiEmptyBox, badge: uiBadge, badgeActive: uiBadgeActive, toast: uiToast, openModal: uiOpenModal, confirmDialog: uiConfirmDialog, chip: uiChip, icon: uiIcon, navIcon: uiNavIcon };

// ------------------------------ screens/dashboard.js ------------------------------
const screenDashboard = {
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

// ------------------------------ screens/products.js ------------------------------
const productsState = { page: 1, totalPages: 1, totalCount: 0, search: "" };
let productsContainerRef = null;

const screenProducts = {
  title: "Товары",

  async render(container) {
    container.replaceChildren(ui.spinner());
    let data;
    try {
      data = await productsLoadPage();
    } catch (err) {
      container.replaceChildren(ui.errorBox(err.message, () => this.render(container)));
      return;
    }
    container.replaceChildren(productsBuildPage(container, data));
  },
};

function productsBuildPage(container, data) {
  return ui.h("div", {},
    ui.h("div", { class: "toolbar" },
      ui.h("div", { class: "search-box", html: ui.icon("search", 15) },
        ui.h("input", {
          type: "search", placeholder: "Поиск: название или SKU…",
          value: productsState.search,
          oninput: (e) => { productsState.search = e.target.value; },
          onkeydown: (e) => { if (e.key === "Enter") { productsState.page = 1; appGo("Products"); } },
        })),
      ui.h("button", { class: "btn", onclick: () => { productsState.page = 1; appGo("Products"); } }, "Найти"),
      ui.h("span", { class: "spacer" }),
      productsCanEdit() ? ui.h("button", { class: "btn btn-primary", onclick: () => openProductModal() },
        ui.h("span", { html: ui.icon("plus", 14) }), "Добавить товар") : null),
    productsRenderTable(data),
    productsRenderPagination());
}

async function productsLoadPage() {
  let url = `api/v1/products?page=${productsState.page}&pageSize=${uiPageSize()}`;
  if (productsState.search.trim()) url += "&search=" + encodeURIComponent(productsState.search.trim());
  const data = await bridge.get(url);
  productsState.totalPages = data.totalPages ?? 1;
  productsState.totalCount = data.totalCount ?? 0;
  if (productsState.page > productsState.totalPages) { productsState.page = Math.max(1, productsState.totalPages); return productsLoadPage(); }
  return data;
}

function productsCanEdit() {
  const role = appState?.user?.role;
  return role === "Administrator" || role === "Manager";
}
function productsCanDelete() {
  return appState?.user?.role === "Administrator";
}

function productsRenderTable(data) {
  const items = data.items || [];
  if (!items.length) return ui.emptyBox(productsState.search ? "Ничего не найдено по запросу." : "Товаров пока нет");

  const rows = items.map((p) => ui.h("tr", { class: "clickable", onclick: () => appGo("ProductCard", { id: p.id }) },
    ui.h("td", { class: "muted" }, p.sku),
    ui.h("td", { class: "strong" }, p.name),
    ui.h("td", { class: "muted" }, p.categoryName || "—"),
    ui.h("td", { class: "num muted" }, p.purchasePrice != null ? ui.money(p.purchasePrice) : "—"),
    ui.h("td", { class: "num" }, ui.money(p.salePrice)),
    ui.h("td", { class: "num" }, String(p.stockTotal ?? 0)),
    ui.h("td", {}, ui.badgeActive(p.isActive)),
    ui.h("td", {}, ui.h("div", { class: "cell-actions" },
      productsCanEdit() ? ui.h("button", { class: "btn btn-ghost btn-sm icon-btn", title: "Редактировать",
        onclick: (e) => { e.stopPropagation(); openProductModal(p); } }, ui.h("span", { html: ui.icon("edit", 14) })) : null,
      productsCanDelete() && p.isActive ? ui.h("button", { class: "btn btn-danger-ghost btn-sm icon-btn", title: "Деактивировать",
        onclick: (e) => { e.stopPropagation(); deactivateProduct(p); } }, ui.h("span", { html: ui.icon("trash", 14) })) : null))));

  return ui.h("div", { class: "table-wrap" },
    ui.h("table", { class: "tbl" },
      ui.h("thead", {}, ui.h("tr", {},
        ui.h("th", {}, "SKU"), ui.h("th", {}, "Название"), ui.h("th", {}, "Категория"),
        ui.h("th", { class: "num" }, "Закупка"), ui.h("th", { class: "num" }, "Продажа"),
        ui.h("th", { class: "num" }, "Остаток"), ui.h("th", {}, "Статус"), ui.h("th", {}))),
      ui.h("tbody", {}, rows)));
}

function productsRenderPagination() {
  return ui.h("div", { class: "pagination" },
    ui.h("span", {}, `Всего: ${productsState.totalCount} · стр. ${productsState.page} из ${productsState.totalPages}`),
    ui.h("div", { class: "pages" },
      ui.h("button", { class: "btn btn-sm", disabled: productsState.page <= 1, onclick: () => { productsState.page--; appGo("Products"); } }, "←"),
      ui.h("button", { class: "btn btn-sm", disabled: productsState.page >= productsState.totalPages, onclick: () => { productsState.page++; appGo("Products"); } }, "→")));
}

async function openProductModal(product) {
  let categories = [];
  try { categories = (await bridge.get("api/v1/categories")) || []; } catch {}

  const editing = Boolean(product);
  const editingProduct = product;
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
    appGo("Products");
  } catch (err) {
    ui.toast(err.message, "error");
  }
}

async function deactivateProduct(p) {
  const ok = await ui.confirmDialog(`Деактивировать товар «${p.name}»? Он исчезнет из продаж.`);
  if (!ok) return;
  try {
    await bridge.del(`api/v1/products/${p.id}`);
    ui.toast("Товар деактивирован", "success");
    appGo("Products");
  } catch (err) {
    ui.toast(err.message, "error");
  }
}

// ------------------------------ screens/productCard.js ------------------------------
const screenProductCard = {
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
      onclick: () => appGo("Products"),
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

    if (productCardCanEdit()) {
      const actions = ui.h("div", { class: "form-actions", style: "margin-top:1.5rem" },
        ui.h("button", { class: "btn", onclick: () => appGo("Products") }, "Назад"),
        ui.h("button", { class: "btn btn-primary", onclick: () => openEditModal(product) },
          ui.h("span", { html: ui.icon("edit", 14) }), " Редактировать"));
      container.append(actions);
    }
  },
};

function productCardCanEdit() {
  const role = appState?.user?.role;
  return role === "Administrator" || role === "Manager";
}

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
    appGo("ProductCard", { id: product.id });
  } catch (err) {
    ui.toast(err.message, "error");
  }
}

// ------------------------------ screens/sales.js ------------------------------
const screenSales = {
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
      items: [],
      searchResults: [],
      searchQuery: "",
    };

    ui.clear(container);
    const el = salesBuildPage(container, state, warehouses, products.items || []);
    container.append(el);
  },
};

function salesBuildPage(container, state, warehouses, allProducts) {
  const wrap = ui.h("div", {});

  const toolbar = ui.h("div", { class: "toolbar" },
    ui.h("div", { class: "form-field", style: "width:200px" },
      ui.h("label", {}, "Склад"),
      ui.h("select", {
        onchange: (e) => { state.warehouseId = e.target.value; salesUpdateCompleteBtn(); },
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
        salesRenderSearchResults(state, allProducts, resultsEl);
      },
      onkeydown: (e) => {
        if (e.key === "Enter" && state.searchResults.length > 0) {
          salesAddToCart(state, state.searchResults[0]);
          state.searchQuery = "";
          salesRenderSearchResults(state, allProducts, resultsEl);
          e.target.value = "";
          salesRenderCart(state, cartEl, totalEl);
        }
      },
    }));

  toolbar.append(searchBox);
  wrap.append(toolbar);

  const resultsEl = ui.h("div", { class: "search-results" });
  wrap.append(resultsEl);

  const cartEl = ui.h("div", {});
  const totalEl = ui.h("div", { class: "cart-total" });
  wrap.append(cartEl);
  wrap.append(totalEl);

  salesRenderCart(state, cartEl, totalEl);

  const completeBtn = ui.h("button", {
    class: "btn btn-primary btn-block",
    style: "margin-top:1rem",
    disabled: true,
    onclick: () => salesCompleteSale(state, warehouses),
  }, "Провести продажу");
  wrap.append(completeBtn);

  return wrap;
}

function salesRenderSearchResults(state, allProducts, el) {
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
        salesAddToCart(state, p);
        state.searchQuery = "";
        salesRenderSearchResults(state, allProducts, el);
        document.querySelector(".search-box input").value = "";
      },
    },
      ui.h("span", { class: "strong" }, p.name),
      ui.h("span", { class: "muted" }, p.sku),
      ui.h("span", { class: "num" }, ui.money(p.salePrice))));
  }
}

function salesAddToCart(state, product) {
  state.items.push({
    productId: product.id,
    sku: product.sku,
    name: product.name,
    price: product.salePrice,
    quantity: 1,
    discount: 0,
  });
  const cartEl = document.querySelector(".cart-table-wrap");
  const totalEl = document.querySelector(".cart-total");
  if (cartEl) salesRenderCart(state, cartEl.parentElement, totalEl);
}

function salesRenderCart(state, container, totalEl) {
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
          ui.h("button", { class: "btn btn-sm btn-ghost", onclick: () => { salesChangeQty(state, idx, -1); salesRenderCart(state, container, totalEl); } }, "−"),
          ui.h("span", {}, String(item.quantity)),
          ui.h("button", { class: "btn btn-sm btn-ghost", onclick: () => { salesChangeQty(state, idx, 1); salesRenderCart(state, container, totalEl); } }, "+"))),
      ui.h("td", {},
        ui.h("input", {
          type: "number", step: "0.01", min: "0",
          class: "inp-sm",
          value: item.discount || "",
          placeholder: "0",
          onchange: (e) => { item.discount = Number(e.target.value) || 0; salesRenderCart(state, container, totalEl); },
        })),
      ui.h("td", { class: "num strong" }, ui.money(subtotal)),
      ui.h("td", {},
        ui.h("button", {
          class: "btn btn-danger-ghost btn-sm icon-btn",
          onclick: () => { state.items.splice(idx, 1); salesRenderCart(state, container, totalEl); },
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

  const completeBtn = container.parentElement.querySelector(".btn-block");
  if (completeBtn) completeBtn.disabled = state.items.length === 0 || !state.warehouseId;
}

function salesChangeQty(state, idx, delta) {
  const item = state.items[idx];
  item.quantity = Math.max(1, item.quantity + delta);
}

function salesUpdateCompleteBtn() {
  const btn = document.querySelector(".btn-block");
  if (btn) {
    const container = btn.closest("div");
    const rows = container?.querySelector("tbody")?.children.length || 0;
    btn.disabled = rows === 0;
  }
}

async function salesCompleteSale(state, warehouses) {
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
    appGo("Sales");
  } catch (err) {
    ui.toast(err.message, "error");
  }
}

// ------------------------------ screens/history.js ------------------------------
const historyState = { page: 1, totalPages: 1, totalCount: 0 };

const screenHistory = {
  title: "История продаж",

  async render(container) {
    container.replaceChildren(ui.spinner());
    let data;
    try {
      data = await historyLoadPage();
    } catch (err) {
      container.replaceChildren(ui.errorBox(err.message, () => this.render(container)));
      return;
    }
    container.replaceChildren(historyBuildPage(container, data));
  },
};

async function historyLoadPage() {
  const url = `api/v1/sales?page=${historyState.page}&pageSize=${uiPageSize()}`;
  const data = await bridge.get(url);
  historyState.totalPages = data.totalPages ?? 1;
  historyState.totalCount = data.totalCount ?? 0;
  if (historyState.page > historyState.totalPages) { historyState.page = Math.max(1, historyState.totalPages); return historyLoadPage(); }
  return data;
}

function historyBuildPage(container, data) {
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
    historyRenderPagination());
}

function historyRenderPagination() {
  return ui.h("div", { class: "pagination" },
    ui.h("span", {}, `Всего: ${historyState.totalCount} · стр. ${historyState.page} из ${historyState.totalPages}`),
    ui.h("div", { class: "pages" },
      ui.h("button", {
        class: "btn btn-sm",
        disabled: historyState.page <= 1,
        onclick: () => { historyState.page--; appGo("SalesHistory"); },
      }, "←"),
      ui.h("button", {
        class: "btn btn-sm",
        disabled: historyState.page >= historyState.totalPages,
        onclick: () => { historyState.page++; appGo("SalesHistory"); },
      }, "→")));
}

// ------------------------------ screens/returns.js ------------------------------
const returnsState = { page: 1, totalPages: 1, totalCount: 0 };

const screenReturns = {
  title: "Возвраты",

  async render(container) {
    container.replaceChildren(ui.spinner());
    let data;
    try {
      data = await returnsLoadPage();
    } catch (err) {
      container.replaceChildren(ui.errorBox(err.message, () => this.render(container)));
      return;
    }
    container.replaceChildren(returnsBuildPage(container, data));
  },
};

async function returnsLoadPage() {
  let url = `api/v1/returns?page=${returnsState.page}&pageSize=${uiPageSize()}`;
  const data = await bridge.get(url);
  returnsState.totalPages = data.totalPages ?? 1;
  returnsState.totalCount = data.totalCount ?? 0;
  if (returnsState.page > returnsState.totalPages) { returnsState.page = Math.max(1, returnsState.totalPages); return returnsLoadPage(); }
  return data;
}

function returnsCanCreate() {
  const role = appState?.user?.role;
  return role === "Administrator" || role === "Manager";
}

function returnsBuildPage(container, data) {
  const items = data.items || [];
  const wrap = ui.h("div", {});

  if (returnsCanCreate()) {
    wrap.append(ui.h("div", { class: "toolbar" },
      ui.h("button", { class: "btn btn-primary", onclick: () => returnsOpenModal() },
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

  wrap.append(returnsRenderPagination());
  return wrap;
}

function returnsRenderPagination() {
  return ui.h("div", { class: "pagination" },
    ui.h("span", {}, `Всего: ${returnsState.totalCount} · стр. ${returnsState.page} из ${returnsState.totalPages}`),
    ui.h("div", { class: "pages" },
      ui.h("button", {
        class: "btn btn-sm",
        disabled: returnsState.page <= 1,
        onclick: () => { returnsState.page--; appGo("Returns"); },
      }, "←"),
      ui.h("button", {
        class: "btn btn-sm",
        disabled: returnsState.page >= returnsState.totalPages,
        onclick: () => { returnsState.page++; appGo("Returns"); },
      }, "→")));
}

async function returnsOpenModal() {
  let sales = [];
  try {
    const data = await bridge.get("api/v1/sales?pageSize=50");
    sales = data.items || [];
  } catch {}

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
      { label: "Создать возврат", cls: "btn-primary", onClick: () => returnsSave(f, close) },
    ],
  });
}

async function returnsSave(f, close) {
  const saleId = f.sale.value;
  const reason = f.reason.value.trim();

  if (!saleId) { ui.toast("Выберите продажу", "error"); return; }

  const body = { originalSaleId: saleId, reason: reason || null, items: [] };

  try {
    await bridge.post("api/v1/returns", body);
    close();
    ui.toast("Возврат создан", "success");
    appGo("Returns");
  } catch (err) {
    ui.toast(err.message, "error");
  }
}

// ------------------------------ screens/receipts.js ------------------------------
const screenReceipts = {
  title: "Поступления",

  async render(container) {
    container.replaceChildren(ui.spinner());

    if (!receiptsCanManage()) {
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
      lines: [],
      searchQuery: "",
      searchResults: [],
    };

    ui.clear(container);
    container.append(receiptsBuildPage(state, warehouses, products.items || []));
  },
};

function receiptsCanManage() {
  const role = appState?.user?.role;
  return role === "Administrator" || role === "Manager";
}

function receiptsBuildPage(state, warehouses, allProducts) {
  const wrap = ui.h("div", {});

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

  const searchBox = ui.h("div", { class: "search-box", style: "flex:1;max-width:400px", html: ui.icon("search", 15) },
    ui.h("input", {
      type: "search", placeholder: "Поиск товара…",
      oninput: (e) => {
        state.searchQuery = e.target.value;
        receiptsRenderSearch(state, allProducts, resultsEl);
      },
      onkeydown: (e) => {
        if (e.key === "Enter" && state.searchResults.length > 0) {
          receiptsAddLine(state, state.searchResults[0]);
          state.searchQuery = "";
          e.target.value = "";
          receiptsRenderSearch(state, allProducts, resultsEl);
          receiptsRenderLines(state, linesEl, totalEl);
        }
      },
    }));
  toolbar.append(searchBox);
  wrap.append(toolbar);

  const resultsEl = ui.h("div", { class: "search-results" });
  wrap.append(resultsEl);

  const linesEl = ui.h("div", {});
  const totalEl = ui.h("div", {});
  wrap.append(linesEl);
  wrap.append(totalEl);
  receiptsRenderLines(state, linesEl, totalEl);

  wrap.append(ui.h("button", {
    class: "btn btn-primary btn-block",
    style: "margin-top:1rem",
    onclick: () => receiptsSave(state),
  }, "Сохранить поступление"));

  return wrap;
}

function receiptsRenderSearch(state, allProducts, el) {
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
        receiptsAddLine(state, p);
        state.searchQuery = "";
        receiptsRenderSearch(state, allProducts, el);
        const inp = document.querySelector(".search-box input");
        if (inp) inp.value = "";
      },
    },
      ui.h("span", { class: "strong" }, p.name),
      ui.h("span", { class: "muted" }, p.sku)));
  }
}

function receiptsAddLine(state, product) {
  state.lines.push({
    productId: product.id,
    sku: product.sku,
    name: product.name,
    quantity: 1,
    unitCost: product.purchasePrice || 0,
  });
}

function receiptsRenderLines(state, container, totalEl) {
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
        ui.h("input", { type: "number", min: "1", step: "1", class: "inp-sm", value: line.quantity,
          onchange: (e) => { line.quantity = Math.max(1, Number(e.target.value) || 1); receiptsRenderLines(state, container, totalEl); } })),
      ui.h("td", {},
        ui.h("input", { type: "number", min: "0", step: "0.01", class: "inp-sm", value: line.unitCost || "", placeholder: "0",
          onchange: (e) => { line.unitCost = Number(e.target.value) || 0; receiptsRenderLines(state, container, totalEl); } })),
      ui.h("td", { class: "num strong" }, ui.money(lineTotal)),
      ui.h("td", {},
        ui.h("button", { class: "btn btn-danger-ghost btn-sm icon-btn", title: "Удалить",
          onclick: () => { state.lines.splice(idx, 1); receiptsRenderLines(state, container, totalEl); } }, ui.h("span", { html: ui.icon("trash", 14) }))));
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

async function receiptsSave(state) {
  if (!state.warehouseId) { ui.toast("Выберите склад", "error"); return; }
  if (!state.lines.length) { ui.toast("Добавьте хотя бы один товар", "error"); return; }

  const body = {
    warehouseId: state.warehouseId,
    items: state.lines.map((l) => ({ productId: l.productId, quantity: l.quantity, unitCost: l.unitCost })),
  };

  try {
    await bridge.post("api/v1/receipts", body);
    ui.toast("Поступление сохранено", "success");
    appGo("Receipts");
  } catch (err) {
    ui.toast(err.message, "error");
  }
}

// ------------------------------ screens/users.js ------------------------------
const usersState = { page: 1, totalPages: 1, totalCount: 0 };

const screenUsers = {
  title: "Пользователи",

  async render(container) {
    container.replaceChildren(ui.spinner());

    if (appState?.user?.role !== "Administrator") {
      container.replaceChildren(ui.errorBox("Доступ только у администраторов."));
      return;
    }

    let data;
    try {
      data = await usersLoadPage();
    } catch (err) {
      container.replaceChildren(ui.errorBox(err.message, () => this.render(container)));
      return;
    }
    container.replaceChildren(usersBuildPage(container, data));
  },
};

async function usersLoadPage() {
  const url = `api/v1/users?page=${usersState.page}&pageSize=${uiPageSize()}`;
  const data = await bridge.get(url);
  usersState.totalPages = data.totalPages ?? 1;
  usersState.totalCount = data.totalCount ?? 0;
  if (usersState.page > usersState.totalPages) { usersState.page = Math.max(1, usersState.totalPages); return usersLoadPage(); }
  return data;
}

function usersBuildPage(container, data) {
  const items = data.items || [];

  const wrap = ui.h("div", {},
    ui.h("div", { class: "toolbar" },
      ui.h("button", { class: "btn btn-primary", onclick: () => usersOpenCreateModal() },
        ui.h("span", { html: ui.icon("plus", 14) }), " Создать пользователя")));

  if (!items.length) {
    wrap.append(ui.emptyBox("Пользователей пока нет"));
    return wrap;
  }

  const rows = items.map((u) => {
    const roleLabel = u.role === "Administrator" ? "Администратор"
      : u.role === "Manager" ? "Менеджер" : u.role === "Cashier" ? "Кассир" : u.role || "—";

    return ui.h("tr", {},
      ui.h("td", { class: "strong" }, u.username),
      ui.h("td", {}, u.displayName || "—"),
      ui.h("td", {}, roleLabel),
      ui.h("td", {}, ui.badgeActive(u.isActive)),
      ui.h("td", {}, u.lastLoginAtUtc ? ui.dateTime(u.lastLoginAtUtc) : "—"),
      ui.h("td", {},
        u.isActive && u.id !== appState?.user?.userId
          ? ui.h("button", { class: "btn btn-danger-ghost btn-sm", onclick: () => usersDeactivate(u) }, "Деактивировать")
          : null));
  });

  wrap.append(ui.h("div", { class: "table-wrap" },
    ui.h("table", { class: "tbl" },
      ui.h("thead", {}, ui.h("tr", {},
        ui.h("th", {}, "Логин"), ui.h("th", {}, "Имя"), ui.h("th", {}, "Роль"), ui.h("th", {}, "Статус"), ui.h("th", {}, "Последний вход"), ui.h("th", {}))),
      ui.h("tbody", {}, rows))));

  wrap.append(usersRenderPagination());
  return wrap;
}

function usersRenderPagination() {
  return ui.h("div", { class: "pagination" },
    ui.h("span", {}, `Всего: ${usersState.totalCount} · стр. ${usersState.page} из ${usersState.totalPages}`),
    ui.h("div", { class: "pages" },
      ui.h("button", { class: "btn btn-sm", disabled: usersState.page <= 1, onclick: () => { usersState.page--; appGo("Users"); } }, "←"),
      ui.h("button", { class: "btn btn-sm", disabled: usersState.page >= usersState.totalPages, onclick: () => { usersState.page++; appGo("Users"); } }, "→")));
}

function usersOpenCreateModal() {
  const f = {
    username: ui.h("input", { type: "text", id: "f-username" }),
    displayName: ui.h("input", { type: "text", id: "f-display" }),
    password: ui.h("input", { type: "password", id: "f-password" }),
    role: ui.h("select", { id: "f-role" },
      ui.h("option", { value: "Cashier" }, "Кассир"),
      ui.h("option", { value: "Manager" }, "Менеджер"),
      ui.h("option", { value: "Administrator" }, "Администратор")),
  };

  const { close } = ui.openModal({
    title: "Новый пользователь",
    body: ui.h("div", {},
      ui.h("div", { class: "form-row" },
        ui.h("div", { class: "form-field" }, ui.h("label", {}, "Логин*"), f.username),
        ui.h("div", { class: "form-field" }, ui.h("label", {}, "Имя"), f.displayName)),
      ui.h("div", { class: "form-row" },
        ui.h("div", { class: "form-field" }, ui.h("label", {}, "Пароль*"), f.password),
        ui.h("div", { class: "form-field" }, ui.h("label", {}, "Роль*"), f.role)),
      ui.h("p", { class: "muted", style: "font-size:0.85rem;margin-top:0.5rem" }, "Рекомендуем сменить пароль при первом входе.")),
    actions: [
      { label: "Отмена", cls: "btn-ghost", onClick: () => {} },
      { label: "Создать", cls: "btn-primary", onClick: () => usersSave(f, close) },
    ],
  });
}

async function usersSave(f, close) {
  const username = f.username.value.trim();
  const password = f.password.value;
  if (!username || !password) { ui.toast("Заполните логин и пароль", "error"); return; }

  try {
    await bridge.post("api/v1/users", { username, displayName: f.displayName.value.trim() || null, password, role: f.role.value });
    close();
    ui.toast("Пользователь создан", "success");
    appGo("Users");
  } catch (err) {
    ui.toast(err.message, "error");
  }
}

async function usersDeactivate(user) {
  const ok = await ui.confirmDialog(`Деактивировать пользователя «${user.username}»?`);
  if (!ok) return;
  try {
    await bridge.del(`api/v1/users/${user.id}`);
    ui.toast("Пользователь деактивирован", "success");
    appGo("Users");
  } catch (err) {
    ui.toast(err.message, "error");
  }
}

// ------------------------------ screens/audit.js ------------------------------
const auditState = { page: 1, totalPages: 1, totalCount: 0 };

const screenAudit = {
  title: "Журнал аудита",

  async render(container) {
    container.replaceChildren(ui.spinner());

    if (appState?.user?.role !== "Administrator") {
      container.replaceChildren(ui.errorBox("Доступ только у администраторов."));
      return;
    }

    let data;
    try {
      data = await auditLoadPage();
    } catch (err) {
      container.replaceChildren(ui.errorBox(err.message, () => this.render(container)));
      return;
    }
    container.replaceChildren(auditBuildPage(container, data));
  },
};

async function auditLoadPage() {
  const url = `api/v1/audit-logs?page=${auditState.page}&pageSize=${uiPageSize()}`;
  const data = await bridge.get(url);
  auditState.totalPages = data.totalPages ?? 1;
  auditState.totalCount = data.totalCount ?? 0;
  if (auditState.page > auditState.totalPages) { auditState.page = Math.max(1, auditState.totalPages); return auditLoadPage(); }
  return data;
}

function auditBuildPage(container, data) {
  const items = data.items || [];
  if (!items.length) return ui.emptyBox("Записей аудита пока нет");

  const rows = items.map((log) =>
    ui.h("tr", {},
      ui.h("td", { class: "muted" }, String(log.id)),
      ui.h("td", {}, ui.dateTime(log.occurredAtUtc)),
      ui.h("td", {}, log.actorUserId ? `#${log.actorUserId}` : "—"),
      ui.h("td", {}, ui.h("span", { class: "badge " + auditActionTone(log.action) }, log.action)),
      ui.h("td", { class: "muted" }, log.entityType || "—"),
      ui.h("td", { class: "muted" }, log.entityId ? `#${log.entityId}` : "—"),
      ui.h("td", { class: "muted small" }, log.correlationId ? log.correlationId.slice(0, 8) + "…" : "—")));

  return ui.h("div", {},
    ui.h("div", { class: "table-wrap" },
      ui.h("table", { class: "tbl" },
        ui.h("thead", {}, ui.h("tr", {},
          ui.h("th", {}, "ID"), ui.h("th", {}, "Дата"), ui.h("th", {}, "Пользователь"), ui.h("th", {}, "Действие"),
          ui.h("th", {}, "Объект"), ui.h("th", {}, "ID объекта"), ui.h("th", {}, "Корреляция"))),
        ui.h("tbody", {}, rows))),
    auditRenderPagination());
}

function auditActionTone(action) {
  const a = (action || "").toLowerCase();
  if (a.includes("delete") || a.includes("deactivat") || a.includes("fail")) return "danger";
  if (a.includes("create") || a.includes("login")) return "success";
  if (a.includes("update") || a.includes("edit") || a.includes("change")) return "warning";
  return "info";
}

function auditRenderPagination() {
  return ui.h("div", { class: "pagination" },
    ui.h("span", {}, `Всего: ${auditState.totalCount} · стр. ${auditState.page} из ${auditState.totalPages}`),
    ui.h("div", { class: "pages" },
      ui.h("button", { class: "btn btn-sm", disabled: auditState.page <= 1, onclick: () => { auditState.page--; appGo("AuditLog"); } }, "←"),
      ui.h("button", { class: "btn btn-sm", disabled: auditState.page >= auditState.totalPages, onclick: () => { auditState.page++; appGo("AuditLog"); } }, "→")));
}

// ------------------------------ screens/settings.js ------------------------------
const SETTINGS_DEFAULTS = { pageSize: "20", sidebarCollapsed: "false", theme: "dark" };

function loadSettings() {
  const raw = localStorage.getItem("shelfops_settings");
  return { ...SETTINGS_DEFAULTS, ...(raw ? JSON.parse(raw) : {}) };
}

function uiPageSize() {
  const n = parseInt(loadSettings().pageSize);
  return n > 0 ? n : 20;
}

function saveSettings(settings) {
  localStorage.setItem("shelfops_settings", JSON.stringify(settings));
}

const screenSettings = {
  title: "Настройки",

  render(container) {
    ui.clear(container);

    const settings = loadSettings();

    const f = {
      pageSize: ui.h("select", { id: "s-page-size" },
        ["10", "20", "30", "50", "100"].map((v) =>
          ui.h("option", { value: v, selected: settings.pageSize === v }, v))),
      sidebarCollapsed: ui.h("input", { type: "checkbox", id: "s-sidebar", checked: settings.sidebarCollapsed === "true" }),
    };

    const card = ui.h("div", { class: "card card-pad" },
      ui.h("h3", {}, "Настройки интерфейса"),
      ui.h("div", { class: "form-field" }, ui.h("label", {}, "Размер страницы (по умолчанию)"), f.pageSize),
      ui.h("div", { class: "form-field" }, ui.h("label", { class: "chk" }, f.sidebarCollapsed, ui.h("span", {}, "Сворачивать боковую панель по умолчанию"))),
      ui.h("div", { class: "form-actions", style: "margin-top:1rem" },
        ui.h("button", { class: "btn btn-primary", onclick: () => settingsSaveLocal(f) }, "Сохранить"),
        ui.h("button", { class: "btn btn-ghost", onclick: () => { appGo("Dashboard"); } }, "Отмена")),
      ui.h("div", { class: "muted", style: "margin-top:1rem;font-size:0.85rem;border-top:1px solid var(--border-subtle);padding-top:1rem" },
        ui.h("p", {}, "Версия: 1.0.0"),
        ui.h("p", {}, `Пользователь: ${appState?.user?.username || "—"} (${appState?.user?.role || "—"})`)));

    container.append(card);
  },
};

function settingsSaveLocal(f) {
  const settings = { pageSize: f.pageSize.value, sidebarCollapsed: String(f.sidebarCollapsed.checked), theme: "dark" };
  saveSettings(settings);
  ui.toast("Настройки сохранены", "success");
  appGo("Settings");
}

// ============================================================
// app.js — состояние, роутер, сайдбар, вход/выход.
// ============================================================

const NAV_ITEMS = [
  { key: "Dashboard",    title: "Дашборд",       icon: "dashboard", roles: ["Administrator", "Manager"] },
  { key: "Products",     title: "Товары",        icon: "products" },
  { key: "ProductCard",  title: "Карточка товара", icon: "products", roles: [], hidden: true },
  { key: "Sales",        title: "Продажи",       icon: "sales" },
  { key: "SalesHistory", title: "История продаж", icon: "history" },
  { key: "Returns",      title: "Возвраты",      icon: "returns" },
  { key: "Receipts",     title: "Поступления",   icon: "receipts" },
  { key: "Users",        title: "Пользователи",  icon: "users", roles: ["Administrator"] },
  { key: "AuditLog",     title: "Журнал аудита", icon: "audit", roles: ["Administrator"] },
  { key: "Settings",     title: "Настройки",     icon: "settings" },
];

const SCREENS = {
  Dashboard: screenDashboard, Products: screenProducts, ProductCard: screenProductCard,
  Sales: screenSales, SalesHistory: screenHistory, Returns: screenReturns,
  Receipts: screenReceipts, Users: screenUsers, AuditLog: screenAudit, Settings: screenSettings,
};

const appState = {
  user: null,
  current: null,
  params: {},
};

const appEl = (id) => document.getElementById(id);

function visibleNavItems() {
  const role = appState.user?.role ?? "";
  return NAV_ITEMS.filter((i) => !i.hidden && (i.roles.length === 0 || i.roles.includes(role)));
}

function defaultScreenKey() {
  const first = visibleNavItems()[0];
  return first ? first.key : "Sales";
}

function currentRoleLabel(role) {
  return role === "Administrator" ? "Администратор"
    : role === "Manager" ? "Менеджер"
    : role === "Cashier" ? "Кассир" : role || "";
}

function renderSidebar() {
  const nav = appEl("sb-nav");
  ui.clear(nav);
  for (const item of visibleNavItems()) {
    const btn = ui.h("button", {
      class: "nav-btn" + (appState.current === item.key ? " active" : ""),
      onclick: () => appGo(item.key),
    },
      ui.h("span", { class: "nav-icon", html: ui.navIcon(item.icon) }),
      ui.h("span", { class: "nav-label" }, item.title));
    nav.append(btn);
  }

  const avatar = document.querySelector(".sb-avatar");
  if (appState.user) {
    avatar.textContent = String(appState.user.displayName || appState.user.username || "?").charAt(0).toUpperCase();
    appEl("sb-user-name").textContent = appState.user.displayName || appState.user.username;
    appEl("sb-user-role").textContent = currentRoleLabel(appState.user.role);
    appEl("topbar-user-name").textContent = appState.user.username;
  }
}

function setSidebarCollapsed(collapsed) {
  appState.sidebarCollapsed = collapsed;
  appEl("sidebar").classList.toggle("collapsed", collapsed);
  appEl("collapse-icon").innerHTML = ui.icon(collapsed ? "plus" : "minus", 15);
}

async function appGo(key, params) {
  params = params || {};
  appState.current = key;
  appState.params = params;
  renderSidebar();

  const screen = SCREENS[key];
  if (!screen) return;

  appEl("topbar-title").textContent = screen.title ?? "";
  const content = ui.clear(appEl("content"));
  content.append(ui.spinner());
  try {
    await screen.render(content, params);
  } catch (err) {
    ui.clear(content).append(ui.errorBox(err.message, () => appGo(key, params)));
  }
}

function showLogin(message) {
  appState.user = null;
  appEl("shell").classList.add("hidden");
  appEl("screen-login").classList.remove("hidden");
  if (message) {
    const err = appEl("login-error");
    err.textContent = message;
    err.classList.remove("hidden");
  }
  appEl("login-username").focus();
}

function showShell() {
  appEl("screen-login").classList.add("hidden");
  appEl("shell").classList.remove("hidden");
  renderSidebar();
  appGo(defaultScreenKey());
}

async function doLogin(username, password) {
  const errBox = appEl("login-error");
  appEl("login-loading").classList.remove("hidden");
  appEl("login-submit").disabled = true;
  try {
    const data = await bridge.post("api/v1/auth/login", { username, password });
    appState.user = data;
    errBox.classList.add("hidden");
    showShell();
  } catch (err) {
    errBox.textContent = err.status === 401 ? "Неверное имя пользователя или пароль." : err.message;
    errBox.classList.remove("hidden");
  } finally {
    appEl("login-loading").classList.add("hidden");
    appEl("login-submit").disabled = false;
  }
}

function doLogout() {
  bridge.logout();
  showLogin();
}

function triggerLogin() {
  const username = appEl("login-username").value.trim();
  const password = appEl("login-password").value;
  if (!username || !password) {
    appEl("login-error").textContent = "Введите имя пользователя и пароль.";
    appEl("login-error").classList.remove("hidden");
    return;
  }
  doLogin(username, password);
}

// ── INIT ──
// Bind all UI elements after DOM is ready
document.addEventListener("DOMContentLoaded", function() {
  // Title bar
  appEl("tb-minimize").addEventListener("click", () => bridge.win("minimize"));
  appEl("tb-maximize").addEventListener("click", () => bridge.win("maximize"));
  appEl("tb-close").addEventListener("click", () => bridge.win("close"));

  // Login
  appEl("login-submit").addEventListener("click", triggerLogin);
  appEl("login-username").addEventListener("keydown", (e) => { if (e.key === "Enter") appEl("login-password").focus(); });
  appEl("login-password").addEventListener("keydown", (e) => { if (e.key === "Enter") triggerLogin(); });

  // Shell
  appEl("sb-logout").addEventListener("click", doLogout);
  appEl("sb-collapse").addEventListener("click", () => setSidebarCollapsed(!appState.sidebarCollapsed));
  setSidebarCollapsed(false);

  bridge.onSessionExpired(() => showLogin("Сессия истекла. Войдите снова."));

  // Remove diagnostic
  document.title = document.title.replace(" ✓", "");
  document.title = document.title.replace(" ✗", "");

  console.log("ShelfOps UI initialized");
});

// Make appGo globally accessible for inline handlers that reference it
window.appGo = appGo;