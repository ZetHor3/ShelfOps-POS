using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using ShelfOps.Desktop.Services;

namespace ShelfOps.Desktop.Views;

public partial class ShellView : Window
{
    private WebBridge? _bridge;
    private readonly ApiClient _api;

    public ShellView(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await InitWebViewAsync();
    }

    private async Task InitWebViewAsync()
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ShelfOps", "WebView2");

        var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
        await Browser.EnsureCoreWebView2Async(env);

        var web = Browser.CoreWebView2;
        Browser.DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 13, 13, 13);
        web.Settings.IsStatusBarEnabled = false;
        web.Settings.AreDefaultContextMenusEnabled = false;
        web.Settings.AreDevToolsEnabled = false;
        web.Settings.IsPasswordAutosaveEnabled = false;
        web.Settings.IsGeneralAutofillEnabled = false;
        web.Settings.IsPinchZoomEnabled = false;

        // Kill every "web page" behavior so this feels like a native app:
        // no find (Ctrl+F), no reload (F5/Ctrl+R), no zoom keyboard shortcuts,
        // no devtools, no address bar — all disabled via AreBrowserAcceleratorKeysEnabled.
        // IsZoomControlEnabled prevents mouse-wheel zoom (Ctrl+wheel, pinch).
        web.Settings.IsZoomControlEnabled = false;
        web.Settings.AreBrowserAcceleratorKeysEnabled = false;

        // Use virtual hostname mapping instead of WebResourceRequested because
        // the WebView2 SDK tries to establish a real HTTPS connection for
        // https:// URLs before firing the WebResourceRequested event, causing
        // ERR_CONNECTION_CLOSED. SetVirtualHostNameToFolderMapping bypasses the
        // network stack entirely.
        var webUiPath = ExtractEmbeddedWebResources();
        web.SetVirtualHostNameToFolderMapping("appassets.local", webUiPath, CoreWebView2HostResourceAccessKind.Allow);

        // Create bridge with the captured CoreWebView2 reference
        _bridge = new WebBridge(_api, web);
        web.WebMessageReceived += OnWebMessageReceived;

        web.Navigate("https://appassets.local/index.html");
    }

    private static string ExtractEmbeddedWebResources()
    {
        var targetDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ShelfOps", "WebUI");

        // Always re-extract so CSS/JS changes from a rebuild take effect.
        if (Directory.Exists(targetDir))
            Directory.Delete(targetDir, recursive: true);
        Directory.CreateDirectory(targetDir);

        var asm = Assembly.GetExecutingAssembly();
        foreach (var resourceName in asm.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith("ShelfOps.Desktop.Web.", StringComparison.OrdinalIgnoreCase))
                continue;

            var relative = resourceName["ShelfOps.Desktop.Web.".Length..];
            var lastDot = relative.LastIndexOf('.');
            if (lastDot < 0) continue;

            var dirPart = relative[..lastDot];
            var extPart = relative[lastDot..];

            var fileDir = dirPart.Replace('.', Path.DirectorySeparatorChar);
            var filePath = Path.Combine(targetDir, fileDir + extPart);

            var dir = Path.GetDirectoryName(filePath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream == null) continue;

            using var fileStream = File.Create(filePath);
            stream.CopyTo(fileStream);
        }

        return targetDir;
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var json = e.TryGetWebMessageAsString();
        if (!string.IsNullOrEmpty(json))
            _bridge?.Handle(json);
    }
}