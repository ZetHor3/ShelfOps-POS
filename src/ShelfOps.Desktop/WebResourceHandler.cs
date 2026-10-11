using System.IO;
using System.Reflection;
using Microsoft.Web.WebView2.Core;

namespace ShelfOps.Desktop;

/// <summary>
/// Serves the embedded Web UI (HTML/CSS/JS) to WebView2 over a virtual
/// origin (https://appassets.local). Resources live inside the assembly as
/// EmbeddedResources ("ShelfOps.Desktop.Web.*") — literally inside the .exe.
///
/// Files are extracted to a local folder on first run, then served via
/// WebResourceRequested with correct MIME types (critical for ES modules,
/// because Windows registers .js as "text/plain" which blocks type="module").
/// </summary>
public class WebResourceHandler
{
    private const string Origin = "https://appassets.local/";
    private static readonly Assembly Asm = typeof(WebResourceHandler).Assembly;
    private readonly string _webUiPath;
    private readonly CoreWebView2Environment _env;
    private bool _extracted;

    public WebResourceHandler(CoreWebView2Environment env)
    {
        _env = env;
        _webUiPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ShelfOps", "WebUI");
    }

    /// <summary>
    /// Ensure files are extracted, then return a response with correct MIME type.
    /// Called from the CoreWebView2.WebResourceRequested event.
    /// </summary>
    public CoreWebView2WebResourceResponse? Respond(string url)
    {
        if (!url.StartsWith(Origin, StringComparison.OrdinalIgnoreCase))
            return null;

        EnsureExtracted();

        var relative = url[Origin.Length..].TrimStart('/');
        if (relative.Length == 0) relative = "index.html";

        var filePath = Path.Combine(_webUiPath, relative.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(filePath))
            return null;

        var contentType = MimeFor(relative);
        var headers = "content-type: " + contentType;
        if (contentType.StartsWith("text/", StringComparison.Ordinal) || contentType.Contains("javascript", StringComparison.Ordinal) ||
            contentType.Contains("json", StringComparison.Ordinal) || contentType.Contains("xml", StringComparison.Ordinal) || contentType.Contains("svg", StringComparison.Ordinal))
            headers += "; charset=utf-8";

        var stream = File.OpenRead(filePath);
        return _env.CreateWebResourceResponse(stream, 200, "OK", headers);
    }

    private void EnsureExtracted()
    {
        if (_extracted) return;
        _extracted = true;

        var markerFile = Path.Combine(_webUiPath, ".extracted");
        if (File.Exists(markerFile)) return;

        if (Directory.Exists(_webUiPath))
            Directory.Delete(_webUiPath, recursive: true);
        Directory.CreateDirectory(_webUiPath);

        foreach (var resourceName in Asm.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith("ShelfOps.Desktop.Web.", StringComparison.OrdinalIgnoreCase))
                continue;

            var relative = resourceName["ShelfOps.Desktop.Web.".Length..];
            var lastDot = relative.LastIndexOf('.');
            if (lastDot < 0) continue;

            var dirPart = relative[..lastDot];
            var extPart = relative[lastDot..];

            var fileDir = dirPart.Replace('.', Path.DirectorySeparatorChar);
            var filePath = Path.Combine(_webUiPath, fileDir + extPart);

            var dir = Path.GetDirectoryName(filePath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using var stream = Asm.GetManifestResourceStream(resourceName);
            if (stream == null) continue;

            using var fileStream = File.Create(filePath);
            stream.CopyTo(fileStream);
        }

        File.WriteAllText(markerFile, "extracted");
    }

    private static string MimeFor(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".html" or ".htm" => "text/html",
            ".css" => "text/css",
            ".js" or ".mjs" => "text/javascript",
            ".json" => "application/json",
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".ico" => "image/x-icon",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".ttf" => "font/ttf",
            ".map" => "application/json",
            _ => "application/octet-stream",
        };
    }
}