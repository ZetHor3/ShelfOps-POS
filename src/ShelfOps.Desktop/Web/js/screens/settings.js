// settings.js — локальные настройки (UI-only, хранятся в localStorage).
import * as ui from "../ui.js";
import * as app from "../app.js";

const DEFAULTS = {
  pageSize: "20",
  sidebarCollapsed: "false",
  theme: "dark",
};

function loadSettings() {
  const raw = localStorage.getItem("shelfops_settings");
  return { ...DEFAULTS, ...(raw ? JSON.parse(raw) : {}) };
}

function saveSettings(settings) {
  localStorage.setItem("shelfops_settings", JSON.stringify(settings));
}

export default {
  title: "Настройки",

  render(container) {
    ui.clear(container);

    const settings = loadSettings();

    const f = {
      pageSize: ui.h("select", { id: "s-page-size" },
        ["10", "20", "30", "50", "100"].map((v) =>
          ui.h("option", { value: v, selected: settings.pageSize === v }, v))),
      sidebarCollapsed: ui.h("input", {
        type: "checkbox", id: "s-sidebar",
        checked: settings.sidebarCollapsed === "true",
      }),
    };

    const card = ui.h("div", { class: "card card-pad" },
      ui.h("h3", {}, "Настройки интерфейса"),
      ui.h("div", { class: "form-field" },
        ui.h("label", {}, "Размер страницы (по умолчанию)"),
        f.pageSize),
      ui.h("div", { class: "form-field" },
        ui.h("label", { class: "chk" },
          f.sidebarCollapsed,
          ui.h("span", {}, "Сворачивать боковую панель по умолчанию"))),
      ui.h("div", { class: "form-actions", style: "margin-top:1rem" },
        ui.h("button", {
          class: "btn btn-primary",
          onclick: () => saveLocalSettings(f),
        }, "Сохранить"),
        ui.h("button", {
          class: "btn btn-ghost",
          onclick: () => { app.go("Dashboard"); },
        }, "Отмена")),
      ui.h("div", { class: "muted", style: "margin-top:1rem;font-size:0.85rem;border-top:1px solid var(--border-subtle);padding-top:1rem" },
        ui.h("p", {}, `Версия: 1.0.0`),
        ui.h("p", {}, `Пользователь: ${app.state.user?.username || "—"} (${app.state.user?.role || "—"})`)));

    container.append(card);
  },
};

function saveLocalSettings(f) {
  const settings = {
    pageSize: f.pageSize.value,
    sidebarCollapsed: String(f.sidebarCollapsed.checked),
    theme: "dark",
  };
  saveSettings(settings);
  ui.toast("Настройки сохранены", "success");
  app.go("Settings");
}