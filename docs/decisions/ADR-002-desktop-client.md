# ADR-002: WPF Desktop Client

**Status:** Superseded by [ADR-004](ADR-004-webview2-desktop.md)  
**Date:** 2026-10-11  
**Deciders:** Project owner (portfolio project)  

## Context

ShelfOps needs a desktop client for store cashiers and managers. The requirements (from technical specification):
- Rich desktop interface (not a web app)
- Dark graphite theme with amber accents
- Real-time-ish interactions (20-30 sales/hour — no hard real-time constraints)
- Cross-platform is not a requirement (Windows-only for store ops)

Options considered:
1. **WPF (.NET Framework / .NET Core)** — mature, Windows-native
2. **WinForms** — simpler but dated
3. **Avalonia UI / MAUI** — cross-platform but newer and less stable
4. **Blazor Hybrid** — web-based UI in a desktop shell
5. **Electron / Tauri** — web technologies in a desktop wrapper

## Decision

Use **WPF with .NET 9** (`net9.0-windows10.0.19041.0` target framework).

## Consequences

### Positive
- **Proven technology:** WPF has 15+ years of production use; tooling is mature
- **CommunityToolkit.Mvvm:** first-class MVVM support with source generators (`[ObservableProperty]`, `[RelayCommand]`)
- **Rich styling:** full control over XAML templates, triggers, and resource dictionaries for the dark theme
- **Desktop integration:** proper window management, taskbar, system tray if needed
- **Portfolio value:** WPF is widely used in enterprise Windows environments

### Negative
- **Windows-only:** no cross-platform capability; acceptable per requirements
- **.NET Framework legacy:** older WPF code samples target .NET Framework; adapting to modern .NET is straightforward but requires awareness
- **No automatic updates:** unlike a web app, updates must be distributed manually or via ClickOnce; acceptable for a portfolio project

### Neutral
- **No hot reload:** XAML hot reload is available in recent VS versions but not relied upon
- **CommunityToolkit.Mvvm** is used instead of Prism/Caliburn — lighter dependency, source-generated code

## Technical Details

### Project Configuration
```xml
<TargetFramework>net9.0-windows10.0.19041.0</TargetFramework>
<UseWPF>true</UseWPF>
```

### Key Packages
| Package | Purpose |
|---|---|
| CommunityToolkit.Mvvm (8.x) | MVVM source generators |
| Microsoft.Extensions.DependencyInjection | DI for ViewModel resolution |
| System.Net.Http.Json | Typed HTTP client for API calls |

### Architecture
```
┌──────────────────────────────────────────────────┐
│                  App.xaml.cs                      │
│  - ConfigureServices (DI container)               │
│  - Resolve ShellView as main window               │
└──────────────────┬───────────────────────────────┘
                   │
┌──────────────────▼───────────────────────────────┐
│                ShellView                          │
│  ┌────────────────┐  ┌──────────────────────────┐ │
│  │  Sidebar Menu  │  │  ContentArea (ContentCont)│ │
│  │  - Dashboard   │  │  Navigate<DashboardView> │ │
│  │  - Products    │  │  Navigate<SalesView>     │ │
│  │  - Sales       │  │  etc.                    │ │
│  │  - Returns     │  └──────────────────────────┘ │
│  │  - Settings    │                               │
│  └────────────────┘                               │
└──────────────────┬───────────────────────────────┘
                   │ NavigationService.Navigate<T>()
┌──────────────────▼───────────────────────────────┐
│              ViewModel                            │
│  - [ObservableProperty]                           │
│  - [RelayCommand]                                 │
│  - ApiClient → HTTP → ASP.NET Core API            │
└──────────────────────────────────────────────────┘
```

### Theming
- Background: `#1E1E1E` (dark graphite)
- Surfaces: `#2D2D2D` (cards, panels)
- Accent: `#F5A623` / `#E8922E` (amber)
- Text: `#E0E0E0` (primary), `#9E9E9E` (secondary)
- States: Default → Hover → Pressed → Disabled → Error

### Screen States
Every view supports: Loading, Loaded, Empty, Error (with retry), Unauthorized

## References

- [ADR-001: Modular Monolith](ADR-001-modular-monolith.md) — overall architecture
- [Architecture Overview](../architecture.md) — component diagram