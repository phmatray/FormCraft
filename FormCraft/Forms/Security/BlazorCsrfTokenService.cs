using System.Security.Cryptography;
using Microsoft.JSInterop;

namespace FormCraft;

/// <summary>
/// Blazor-specific implementation of CSRF token service using browser storage.
/// </summary>
public class BlazorCsrfTokenService : ICsrfTokenService
{
    private readonly IJSRuntime _jsRuntime;
    private const string StorageKey = "FormCraft_CsrfToken";
    private const int TokenLength = 32;

    public BlazorCsrfTokenService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string> GenerateTokenAsync()
    {
        var token = GenerateRandomToken();

        // Store token in session storage
        try
        {
            await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", StorageKey, token);
        }
        catch
        {
            // Fallback for when JavaScript is not available (e.g., prerendering)
        }

        return token;
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        try
        {
            var storedToken = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", StorageKey);
            return !string.IsNullOrEmpty(storedToken) && storedToken == token;
        }
        catch
        {
            // Fallback for when JavaScript is not available
            return false;
        }
    }

    private static string GenerateRandomToken()
    {
        var bytes = new byte[TokenLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}