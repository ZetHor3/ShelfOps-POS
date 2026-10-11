// bridge.js — асинхронный мост к C# (WebBridge.cs через window.chrome.webview).
// Токен JWT живёт в памяти C# и никогда не попадает в JS.

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

/** Выполнить API-запрос. Возвращает Promise<данные>; при ошибке — Error со свойством .status. */
export function api(method, path, body) {
  return send({ type: "api", method, path, body: body ?? null });
}
export const get = (path) => api("GET", path);
export const post = (path, body) => api("POST", path, body);
export const put = (path, body) => api("PUT", path, body);
export const del = (path) => api("DELETE", path);

/** Управление окном. */
export function win(action) { return send({ type: "window", action }); }

/** Выход из системы (токен сбрасывается в C#). */
export function logout() { return send({ type: "logout" }); }

/** Регистрирует callback на событие "сессия истекла" (C# прислал 401 на фоне). */
export function onSessionExpired(fn) { sessionExpiredHandler = fn; }

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