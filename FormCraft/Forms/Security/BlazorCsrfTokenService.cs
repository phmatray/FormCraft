using System.Security.Cryptography;
using System.Text;
using Microsoft.JSInterop;

namespace FormCraft;

/// <summary>
/// Blazor-specific implementation of CSRF token service using browser session storage.
/// </summary>
/// <remarks>
/// <para>
/// JavaScript interop is unavailable during Blazor Server prerendering, so a token generated at
/// that point cannot be written to session storage immediately. To keep such tokens validatable,
/// this (scoped) service also remembers the most recently issued token in memory: when session
/// storage is empty or unavailable, <see cref="ValidateTokenAsync"/> falls back to comparing
/// against the in-memory token and persists it to session storage as soon as interop becomes
/// available. A token therefore always validates within the circuit/scope that issued it.
/// </para>
/// <para>
/// Token comparisons use <see cref="CryptographicOperations.FixedTimeEquals(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>
/// to avoid timing side channels.
/// </para>
/// </remarks>
public class BlazorCsrfTokenService : ICsrfTokenService
{
    private readonly IJSRuntime _jsRuntime;
    private const string StorageKey = "FormCraft_CsrfToken";
    private const int TokenLength = 32;

    private string? _currentToken;
    private bool _isPersisted;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorCsrfTokenService"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime used to access browser session storage.</param>
    public BlazorCsrfTokenService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <inheritdoc />
    public async Task<string> GenerateTokenAsync()
    {
        var token = GenerateRandomToken();

        // Keep the token in memory so it stays validatable even when storage is
        // unavailable right now (e.g., prerendering). See class remarks.
        _currentToken = token;
        _isPersisted = await TryPersistTokenAsync(token);

        return token;
    }

    /// <inheritdoc />
    public async Task<bool> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        string? storedToken = null;
        try
        {
            storedToken = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", StorageKey);
        }
        catch (Exception ex) when (IsJsInteropUnavailable(ex))
        {
            // Session storage unavailable (e.g., prerendering) — fall back to the in-memory token below.
        }

        if (!string.IsNullOrEmpty(storedToken))
            return FixedTimeEquals(storedToken, token);

        // No stored token: accept the token this service instance issued (it may not have been
        // persisted yet because generation happened during prerendering) and try to persist it now.
        if (_currentToken != null && FixedTimeEquals(_currentToken, token))
        {
            if (!_isPersisted)
                _isPersisted = await TryPersistTokenAsync(_currentToken);
            return true;
        }

        return false;
    }

    private async Task<bool> TryPersistTokenAsync(string token)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", StorageKey, token);
            return true;
        }
        catch (Exception ex) when (IsJsInteropUnavailable(ex))
        {
            return false;
        }
    }

    private static bool IsJsInteropUnavailable(Exception ex)
        => ex is JSException or JSDisconnectedException or InvalidOperationException;

    private static bool FixedTimeEquals(string left, string right)
        => CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(left),
            Encoding.UTF8.GetBytes(right));

    private static string GenerateRandomToken()
    {
        var bytes = new byte[TokenLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
