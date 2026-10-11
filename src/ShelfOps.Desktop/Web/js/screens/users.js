// users.js — управление пользователями (только Admin).
import * as bridge from "../bridge.js";
import * as ui from "../ui.js";
import * as app from "../app.js";

const s = { page: 1, totalPages: 1, totalCount: 0 };

export default {
  title: "Пользователи",

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
  const url = `api/v1/users?page=${s.page}&pageSize=${ui.pageSize()}`;
  const data = await bridge.get(url);
  s.totalPages = data.totalPages ?? 1;
  s.totalCount = data.totalCount ?? 0;
  if (s.page > s.totalPages) { s.page = Math.max(1, s.totalPages); return loadPage(); }
  return data;
}

function buildPage(container, data) {
  const items = data.items || [];

  const wrap = ui.h("div", {},
    ui.h("div", { class: "toolbar" },
      ui.h("button", { class: "btn btn-primary", onclick: () => openCreateModal() },
        ui.h("span", { html: ui.icon("plus", 14) }), " Создать пользователя")));

  if (!items.length) {
    wrap.append(ui.emptyBox("Пользователей пока нет"));
    return wrap;
  }

  const rows = items.map((u) => {
    const roleLabel = u.role === "Administrator" ? "Администратор"
      : u.role === "Manager" ? "Менеджер"
      : u.role === "Cashier" ? "Кассир" : u.role || "—";

    return ui.h("tr", {},
      ui.h("td", { class: "strong" }, u.username),
      ui.h("td", {}, u.displayName || "—"),
      ui.h("td", {}, roleLabel),
      ui.h("td", {}, ui.badgeActive(u.isActive)),
      ui.h("td", {}, u.lastLoginAtUtc ? ui.dateTime(u.lastLoginAtUtc) : "—"),
      ui.h("td", {},
        u.isActive && u.id !== app.state.user?.userId
          ? ui.h("button", {
            class: "btn btn-danger-ghost btn-sm",
            onclick: () => deactivateUser(u),
          }, "Деактивировать")
          : null));
  });

  wrap.append(ui.h("div", { class: "table-wrap" },
    ui.h("table", { class: "tbl" },
      ui.h("thead", {}, ui.h("tr", {},
        ui.h("th", {}, "Логин"),
        ui.h("th", {}, "Имя"),
        ui.h("th", {}, "Роль"),
        ui.h("th", {}, "Статус"),
        ui.h("th", {}, "Последний вход"),
        ui.h("th", {}))),
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
        onclick: () => { s.page--; app.go("Users"); },
      }, "←"),
      ui.h("button", {
        class: "btn btn-sm",
        disabled: s.page >= s.totalPages,
        onclick: () => { s.page++; app.go("Users"); },
      }, "→")));
}

/* ── Создание пользователя ── */
function openCreateModal() {
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
      ui.h("p", { class: "muted", style: "font-size:0.85rem;margin-top:0.5rem" },
        "Рекомендуем сменить пароль при первом входе.")),
    actions: [
      { label: "Отмена", cls: "btn-ghost", onClick: () => {} },
      { label: "Создать", cls: "btn-primary", onClick: () => saveUser(f, close) },
    ],
  });
}

async function saveUser(f, close) {
  const username = f.username.value.trim();
  const password = f.password.value;
  if (!username || !password) { ui.toast("Заполните логин и пароль", "error"); return; }

  try {
    await bridge.post("api/v1/users", {
      username,
      displayName: f.displayName.value.trim() || null,
      password,
      role: f.role.value,
    });
    close();
    ui.toast("Пользователь создан", "success");
    app.go("Users");
  } catch (err) {
    ui.toast(err.message, "error");
  }
}

async function deactivateUser(user) {
  const ok = await ui.confirmDialog(`Деактивировать пользователя «${user.username}»?`);
  if (!ok) return;
  try {
    await bridge.del(`api/v1/users/${user.id}`);
    ui.toast("Пользователь деактивирован", "success");
    app.go("Users");
  } catch (err) {
    ui.toast(err.message, "error");
  }
}