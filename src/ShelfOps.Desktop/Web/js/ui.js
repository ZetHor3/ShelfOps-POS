// ui.js — маленькие DOM/формат-хелперы. Без фреймворков, без innerHTML с пользовательскими данными.

const SVG_TAGS = new Set([
  "svg", "path", "rect", "circle", "line", "polyline", "polygon", "g", "text", "tspan",
  "defs", "stop", "use", "ellipse", "mask", "clippath", "pattern", "lineargradient", "radialgradient",
]);

/** Создать элемент: h('div', {class:'x', onclick}, ...children). SVG-теги поддерживаются. */
export function h(tag, attrs = {}, ...children) {
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
  append(el, children);
  return el;
}

export function append(el, children) {
  for (const c of children.flat(Infinity)) {
    if (c === null || c === undefined || c === false) continue;
    el.append(c instanceof Node ? c : document.createTextNode(String(c)));
  }
  return el;
}

export const clear = (el) => { el.replaceChildren(); return el; };

/** Размер страницы из настроек (localStorage), 20 по умолчанию. */
export function pageSize() {
  try {
    const n = parseInt(JSON.parse(localStorage.getItem("shelfops_settings") || "{}").pageSize);
    return n > 0 ? n : 20;
  } catch { return 20; }
}

/** Экранирование для текстовых вставок (при необходимости). */
export function esc(s) {
  return String(s ?? "").replace(/[&<>"']/g, (m) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[m]));
}

/* ── форматирование ── */
export function money(v) {
  const n = Number(v ?? 0);
  return n.toLocaleString("ru-RU", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + " ₽";
}
export function moneyShort(v) {
  const n = Number(v ?? 0);
  return n.toLocaleString("ru-RU", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + " ₽";
}
export function date(d) {
  if (!d) return "—";
  const s = new Date(d);
  if (isNaN(s)) return String(d);
  return s.toLocaleDateString("ru-RU");
}
export function dateTime(d) {
  if (!d) return "—";
  const s = new Date(d);
  if (isNaN(s)) return String(d);
  return s.toLocaleString("ru-RU", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" });
}

/* ── состояние экрана ── */
export function spinner(text = "Загрузка…") {
  return h("div", { class: "page-loading" }, h("div", { class: "spinner" }), h("div", {}, text));
}

export function errorBox(message, retry) {
  return h("div", { class: "page-error" },
    h("div", { class: "big" }, "⚠"),
    h("div", { style: "max-width:480px" }, message),
    retry ? h("button", { class: "btn", onclick: retry }, "Повторить") : null);
}

export function emptyBox(text, big = "📦") {
  return h("div", { class: "page-empty" },
    h("div", { class: "big" }, big),
    h("div", {}, text));
}

/* ── бейджи статусов ── */
const STATUS_TONE = {
  completed: "success", active: "success", активирован: "success", успешно: "success",
  cancelled: "danger", blocked: "danger", заблокирован: "danger", failed: "danger",
  pending: "warning", processing: "warning", неактивен: "muted", inactive: "muted",
  returned: "info", void: "muted",
};

export function badge(text) {
  const key = String(text ?? "").toLowerCase();
  const tone = STATUS_TONE[key] || "muted";
  return h("span", { class: "badge " + tone }, esc(text));
}

export function badgeActive(isActive) {
  return badge(isActive ? "Активен" : "Неактивен");
}

/* ── тосты ── */
export function toast(message, type = "info", ms = 3200) {
  const root = document.getElementById("toast-root");
  const el = h("div", { class: "toast " + type }, message);
  root.append(el);
  setTimeout(() => {
    el.style.transition = "opacity .3s ease, transform .3s ease";
    el.style.opacity = "0";
    el.style.transform = "translateX(16px)";
    setTimeout(() => el.remove(), 320);
  }, ms);
}

/* ── модальные окна ── */
export function openModal({ title, body, actions = [], wide = false, onClose }) {
  const root = document.getElementById("modal-root");
  const modal = h("div", { class: "modal" + (wide ? " wide" : "") },
    h("h3", {}, title),
    body);

  const footer = h("div", { class: "form-actions" });
  for (const a of actions) {
    footer.append(h("button", { class: "btn " + (a.cls || ""), onclick: () => { const r = a.onClick?.(modal); if (a.close !== false) close(); return r; } }, a.label));
  }
  modal.append(footer);

  const overlay = h("div", { class: "modal-overlay", onclick: (e) => { if (e.target === overlay) close(); } }, modal);
  root.append(overlay);

  function close() {
    overlay.remove();
    onClose?.();
  }

  return { el: modal, close };
}

export function confirmDialog(message, title = "Подтверждение") {
  return new Promise((resolve) => {
    openModal({
      title,
      body: h("p", { style: "color:var(--text-secondary)" }, message),
      actions: [
        { label: "Отмена", cls: "btn-ghost", onClick: () => resolve(false) },
        { label: "Удалить", cls: "btn-danger", onClick: () => resolve(true) },
      ],
    });
  });
}

/* ── фильтр-чипсы ── */
export function chip(label, active, onclick) {
  return h("button", { class: "chip" + (active ? " on" : ""), onclick }, label);
}

/* ── иконки (SVG, stroke текущего цвета) ── */
const paths = {
  back: '<path d="M19 12H5"/><polyline points="12 19 5 12 12 5"/>',
  search: '<circle cx="11" cy="11" r="7"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>',
  plus: '<line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>',
  minus: '<line x1="5" y1="12" x2="19" y2="12"/>',
  trash: '<polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/>',
  edit: '<path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>',
  info: '<circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/>',
  close: '<line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>',
};

export function icon(name, size = 16) {
  return `<svg viewBox="0 0 24 24" width="${size}" height="${size}" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">${paths[name] || ""}</svg>`;
}

/* ── навигационные иконки (sidebar) ── */
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
export function navIcon(name, size = 17) {
  return `<svg viewBox="0 0 24 24" width="${size}" height="${size}" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">${navPaths[name] || ""}</svg>`;
}