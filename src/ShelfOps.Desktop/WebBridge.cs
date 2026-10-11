using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using ShelfOps.Desktop.Services;
using ShelfOps.Desktop.Views;

namespace ShelfOps.Desktop;

/// <summary>
/// Bridge between the HTML/JS UI (running inside WebView2) and the C# side.
/// JS sends JSON messages via window.chrome.webview.postMessage; each API
/// call is executed through the app's ApiClient so the JWT stays in memory
/// and never reaches the JS layer.
/// </summary>
public class WebBridge
{
    private readonly ApiClient _api;
    private readonly CoreWebView2 _web;
    private readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>Stores the CoreWebView2 reference once, at init time.</summary>
    public WebBridge(ApiClient api, CoreWebView2 web)
    {
        _api = api;
        _web = web;
    }

    /// <summary>Entry point for CoreWebView2.WebMessageReceived.</summary>
    public async void Handle(string json)
    {
        try
        {
            // Debug log: confirm messages arrive from JS
            File.AppendAllText(Path.Combine(Path.GetTempPath(), "shelfops_bridge.log"),
                $"[{DateTime.UtcNow:O}] Handle received: type= parsing… json={json[..Math.Min(json.Length, 200)]}\n");

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var type = root.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
            var id = root.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.Number
                ? idEl.GetInt64() : 0;

            switch (type)
            {
                case "api":
                    await HandleApiAsync(id, root);
                    break;

                case "window":
                    HandleWindow(root.TryGetProperty("action", out var actionEl) ? actionEl.GetString() : null);
                    break;

                case "logout":
                    _api.Token = null;
                    break;
            }
        }
        catch (Exception ex)
        {
            TryPost(new { type = "bridge", error = "Bridge parse error: " + ex.Message });
        }
    }

    private async Task HandleApiAsync(long id, JsonElement root)
    {
        var method = (root.TryGetProperty("method", out var mEl) ? mEl.GetString() : "GET")?.ToUpperInvariant();
        var path = root.TryGetProperty("path", out var pEl) ? pEl.GetString() ?? "" : "";

        object? body = null;
        if (root.TryGetProperty("body", out var bodyEl) && bodyEl.ValueKind != JsonValueKind.Null)
            body = bodyEl.Clone();

        // Special-case the login endpoint so the token never reaches the JS layer.
        if (string.Equals(method, "POST", StringComparison.Ordinal) &&
            path.EndsWith("auth/login", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var login = JsonSerializer.Deserialize<LoginRequest>(body?.ToString() ?? "{}", _json);
                if (login == null || string.IsNullOrWhiteSpace(login.Username))
                    throw new InvalidOperationException("Пустое имя пользователя");

                var result = await _api.LoginAsync(login.Username, login.Password ?? "");

                // Debug: confirm we got past LoginAsync
                File.AppendAllText(Path.Combine(Path.GetTempPath(), "shelfops_bridge.log"),
                    $"[{DateTime.UtcNow:O}] LoginAsync OK for user={result.Username} role={result.Role}\n");

                TryPost(new
                {
                    type = "api",
                    id,
                    ok = true,
                    status = 200,
                    data = new
                    {
                        result.Username,
                        result.DisplayName,
                        result.Role,
                        result.UserId
                    }
                });
            }
            catch (UnauthorizedAccessException)
            {
                TryPost(new { type = "api", id, ok = false, status = 401, error = "Неверное имя пользователя или пароль." });
            }
            catch (Exception ex)
            {
                TryPost(new { type = "api", id, ok = false, status = 0, error = ex.Message });
            }
            return;
        }

        try
        {
            var (status, content) = await _api.SendAsync(method ?? "GET", path, body);

            if (status is >= 200 and < 300)
            {
                object? data = null;
                if (!string.IsNullOrEmpty(content))
                    data = JsonSerializer.Deserialize<JsonElement>(content, _json);

                TryPost(new { type = "api", id, ok = true, status, data });
            }
            else
            {
                TryPost(new { type = "api", id, ok = false, status, error = content ?? $"HTTP {status}" });

                // Session expired → tell JS to return to the login screen.
                if (status == 401)
                    TryPost(new { type = "auth", status = "session-expired" });
            }
        }
        catch (Exception ex)
        {
            TryPost(new { type = "api", id, ok = false, status = 0, error = ex.Message });
        }
    }

    private static void HandleWindow(string? action)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var shell = Application.Current.MainWindow as ShellView;
            if (shell == null) return;
            switch (action)
            {
                case "minimize":
                    shell.WindowState = WindowState.Minimized;
                    break;
                case "maximize":
                    shell.WindowState = shell.WindowState == WindowState.Maximized
                        ? WindowState.Normal
                        : WindowState.Maximized;
                    break;
                case "close":
                    shell.Close();
                    break;
            }
        });
    }

    /// <summary>Post back to JS via WebView2. Safe to call from the UI thread only.</summary>
    private void TryPost(object payload)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload, _json);
            File.AppendAllText(Path.Combine(Path.GetTempPath(), "shelfops_bridge.log"),
                $"[{DateTime.UtcNow:O}] TryPost serialized: type={payload.GetType().Name}\n");

            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    File.AppendAllText(Path.Combine(Path.GetTempPath(), "shelfops_bridge.log"),
                        $"[{DateTime.UtcNow:O}] TryPost invoke begin\n");
                    _web.PostWebMessageAsJson(json);
                    File.AppendAllText(Path.Combine(Path.GetTempPath(), "shelfops_bridge.log"),
                        $"[{DateTime.UtcNow:O}] TryPost invoke SUCCESS\n");
                }
                catch (Exception ex)
                {
                    File.AppendAllText(Path.Combine(Path.GetTempPath(), "shelfops_bridge.log"),
                        $"[{DateTime.UtcNow:O}] TryPost invoke EXCEPTION: {ex.Message}\n");
                }
            });
        }
        catch (Exception ex)
        {
            File.AppendAllText(Path.Combine(Path.GetTempPath(), "shelfops_bridge.log"),
                $"[{DateTime.UtcNow:O}] TryPost outer EXCEPTION: {ex}\n");
        }
    }

    private sealed record LoginRequest(string Username, string Password);
}