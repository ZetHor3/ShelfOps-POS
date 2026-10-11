using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ShelfOps.Desktop.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string? Token { get; set; }

    public ApiClient(string baseUrl = "http://localhost:5000")
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private void SetAuthHeader()
    {
        _http.DefaultRequestHeaders.Authorization =
            !string.IsNullOrEmpty(Token)
                ? new AuthenticationHeaderValue("Bearer", Token)
                : null;
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default)
    {
        SetAuthHeader();
        var response = await _http.GetAsync(endpoint, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
    }

    public async Task<T?> PostAsync<T>(string endpoint, object? body = null, CancellationToken ct = default)
    {
        SetAuthHeader();
        var response = await _http.PostAsJsonAsync(endpoint, body, _jsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
    }

    public async Task<T?> PutAsync<T>(string endpoint, object? body = null, CancellationToken ct = default)
    {
        SetAuthHeader();
        var response = await _http.PutAsJsonAsync(endpoint, body, _jsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
    }

    public async Task DeleteAsync(string endpoint, CancellationToken ct = default)
    {
        SetAuthHeader();
        var response = await _http.DeleteAsync(endpoint, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Bridge-friendly request: returns raw (status, content) without throwing,
    /// so the HTML/JS layer can render error states itself.
    /// </summary>
    public async Task<(int Status, string? Content)> SendAsync(
        string method, string path, object? body = null, CancellationToken ct = default)
    {
        SetAuthHeader();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (body != null)
            request.Content = JsonContent.Create(body, null, _jsonOptions);
        using var response = await _http.SendAsync(request, ct);
        var content = response.Content is null ? null : await response.Content.ReadAsStringAsync(ct);
        return ((int)response.StatusCode, content);
    }

    public async Task<LoginResult> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/v1/auth/login",
            new { username, password }, _jsonOptions, ct);

        if (response.IsSuccessStatusCode)
        {
            var loginResp = await response.Content.ReadFromJsonAsync<LoginResult>(_jsonOptions, ct);
            if (loginResp != null)
            {
                Token = loginResp.Token;
                return loginResp;
            }
        }

        var error = await response.Content.ReadAsStringAsync(ct);
        throw new UnauthorizedAccessException(error);
    }
}

public record LoginResult(string Token, string Username, string DisplayName, string Role, Guid UserId, DateTime ExpiresAt);