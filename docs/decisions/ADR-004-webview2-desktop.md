# ADR-004: WebView2 Desktop Client

**Status:** Active (supersedes ADR-002)

## Context

The original desktop client used WPF with CommunityToolkit.Mvvm and XAML views
(ADR-002). After initial implementation, it became clear that:

1. **XAML styling is verbose** — achieving modern glassmorphism effects requires
   complex templates, converters, and custom controls.
2. **CSS is far more expressive** — gradients, backdrop-filter blur, animations,
   responsive layouts are trivial in CSS but painful in XAML.
3. **WebView2 (Edge Chromium) is preinstalled on Windows 11** — no extra runtime
   to ship. The WebView2 control is a first-party NuGet package.

## Decision

Replace ALL XAML views/ViewModels/styles with a **single WPF window hosting
WebView2**. The UI is written in vanilla HTML/CSS/JS, embedded inside the .exe.

### Architecture

```
WPF Window (ShellView.xaml)
  └── WebView2 (fills entire client area)
        ├── index.html + app.css + JS modules (EmbeddedResource)
        ├── window.chrome.webview.postMessage
        └── WebBridge.cs ⇄ ApiClient.cs ⇄ HTTP API
```

### Key design choices

1. **No npm, no frameworks** — vanilla JS (ES modules, supported by WebView2).
   Zero build step, zero dependencies, offline-capable.
2. **JWT token in C# only** — token never reaches JavaScript. All API calls
   go through the C# bridge.
3. **Embedded resources** — all Web UI files compiled into the assembly as
   `EmbeddedResource`, extracted at startup to `%LOCALAPPDATA%\ShelfOps\WebUI\`.
   Served via `SetVirtualHostNameToFolderMapping` on `appassets.local`.
4. **Virtual hostname mapping** — avoids `WebResourceRequested` timing issues
   with HTTPS URLs. No network stack involvement.
5. **Glassmorphism** — CSS `backdrop-filter: blur()` for frosted-glass panels,
   animated gradient background for depth. Fully supported in Chromium/WebView2.

### What was removed

- All XAML Views (LoginView, DashboardView, ProductsView, etc.)
- All ViewModels (12 files)
- CommunityToolkit.Mvvm package
- DarkTheme.xaml, AmberAccent styles
- ContentControl-based navigation service

### What was kept

- `ApiClient.cs` — HTTP client with in-memory token
- `ShellView.xaml(.cs)` — rewritten: now hosts only WebView2
- `App.xaml(.cs)` — simplified to DI setup for `ApiClient` + `ShellView`
- Backend (`ShelfOps.Api`) — untouched

## Consequences

**Positive:**
- Rich UI with ~10x less code than equivalent XAML
- Glassmorphism, animations, responsive layout — zero effort
- Single-file CSS custom properties for global theming
- No XAML compilation, no designer overhead
- Full developer control over every pixel via CSS

**Negative:**
- WebView2 dependency (~300 KB NuGet package, runtime uses Edge Chromium)
- Slightly larger memory footprint than native WPF
- Debugging requires browser devtools (separate window)
- Cannot render while WebView2 is initializing (async)

**Neutral:**
- HTML/CSS/JS skills are more common than WPF/XAML skills
- WebView2 API surface is different from WPF — new learning curve for bridge
- Screen reader / accessibility story differs from native WPF

## Related

- [ADR-001: Modular Monolith](ADR-001-modular-monolith.md)
- [ADR-002: WPF Desktop Client](ADR-002-desktop-client.md) (superseded)