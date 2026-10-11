// app.js — точка входа: состояние, роутер, сайдбар, вход/выход.
import * as bridge from "./bridge.js";
import * as ui from "./ui.js";

import dashboard from "./screens/dashboard.js";
import products from "./screens/products.js";
import productCard from "./screens/productCard.js";
import sales from "./screens/sales.js";
import history from "./screens/history.js";
import returns from "./screens/returns.js";
import receipts from "./screens/receipts.js";
import users from "./screens/users.js";
import audit from "./screens/audit.js";
import settings from "./screens/settings.js";

/* ── Пункты меню ── */
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
  Dashboard, Products, ProductCard: productCard, Sales: sales,
  SalesHistory: history, Returns: returns, Receipts: receipts,
  Users: users, AuditLog: audit, Settings: settings,
};

const state = {
  user: null,              // { username, displayName, role, userId }
  current: null,
  params: {},
  sidebarCollapsed: false,
};

const $ = (id) => document.getElementById(id);

function visibleNavItems() {
  const role = state.user?.role ?? "";
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

/* ── САЙДБАР ── */
function renderSidebar() {
  const nav = $("sb-nav");
  ui.clear(nav);
  for (const item of visibleNavItems()) {
    const btn = ui.h("button", {
      class: "nav-btn" + (state.current === item.key ? " active" : ""),
      onclick: () => go(item.key),
    },
      ui.h("span", { class: "nav-icon", html: ui.navIcon(item.icon) }),
      ui.h("span", { class: "nav-label" }, item.title));
    nav.append(btn);
  }

  const avatar = document.querySelector(".sb-avatar");
  if (state.user) {
    avatar.textContent = String(state.user.displayName || state.user.username || "?").charAt(0).toUpperCase();
    $("sb-user-name").textContent = state.user.displayName || state.user.username;
    $("sb-user-role").textContent = currentRoleLabel(state.user.role);
    $("topbar-user-name").textContent = state.user.username;
  }
}

function setSidebarCollapsed(collapsed) {
  state.sidebarCollapsed = collapsed;
  $("sidebar").classList.toggle("collapsed", collapsed);
  $("collapse-icon").innerHTML = ui.icon(collapsed ? "plus" : "minus", 15);
}

/* ── РОУТЕР ── */
async function go(key, params = {}) {
  state.current = key;
  state.params = params;
  renderSidebar();

  const screen = SCREENS[key];
  if (!screen) return;

  $("topbar-title").textContent = screen.title ?? "";
  const content = ui.clear($("content"));
  content.append(ui.spinner());
  try {
    await screen.render(content, params);
  } catch (err) {
    ui.clear(content).append(ui.errorBox(err.message, () => go(key, params)));
  }
}

/* ── ВХОД / ВЫХОД ── */
function showLogin(message) {
  state.user = null;
  $("shell").classList.add("hidden");
  $("screen-login").classList.remove("hidden");
  if (message) {
    const err = $("login-error");
    err.textContent = message;
    err.classList.remove("hidden");
  }
  // ensure focus on username field
  $("login-username").focus();
}

function showShell() {
  $("screen-login").classList.add("hidden");
  $("shell").classList.remove("hidden");
  renderSidebar();
  go(defaultScreenKey());
}

async function doLogin(username, password) {
  const errBox = $("login-error");
  $("login-loading").classList.remove("hidden");
  $("login-submit").disabled = true;
  try {
    const data = await bridge.post("api/v1/auth/login", { username, password });
    state.user = data;
    errBox.classList.add("hidden");
    showShell();
  } catch (err) {
    errBox.textContent = err.status === 401 ? "Неверное имя пользователя или пароль." : err.message;
    errBox.classList.remove("hidden");
  } finally {
    $("login-loading").classList.add("hidden");
    $("login-submit").disabled = false;
  }
}

function doLogout() {
  bridge.logout();
  showLogin();
}

/* ── ИНИЦИАЛИЗАЦИЯ ── */
// Module scripts run after DOM parsing, so elements exist at this point.
// Wire all buttons directly.

// Title bar — window control buttons
$("tb-minimize").addEventListener("click", () => bridge.win("minimize"));
$("tb-maximize").addEventListener("click", () => bridge.win("maximize"));
$("tb-close").addEventListener("click", () => bridge.win("close"));

// Login — click on the button (no <form> to avoid HTML form submission)
function triggerLogin() {
  const username = $("login-username").value.trim();
  const password = $("login-password").value;
  if (!username || !password) {
    $("login-error").textContent = "Введите имя пользователя и пароль.";
    $("login-error").classList.remove("hidden");
    return;
  }
  doLogin(username, password);
}

$("login-submit").addEventListener("click", triggerLogin);

// Enter key submits login
$("login-username").addEventListener("keydown", (e) => { if (e.key === "Enter") $("login-password").focus(); });
$("login-password").addEventListener("keydown", (e) => { if (e.key === "Enter") triggerLogin(); });

// Shell
$("sb-logout").addEventListener("click", doLogout);
$("sb-collapse").addEventListener("click", () => setSidebarCollapsed(!state.sidebarCollapsed));
setSidebarCollapsed(false);

bridge.onSessionExpired(() => showLogin("Сессия истекла. Войдите снова."));

export { bridge, go, state, showLogin };