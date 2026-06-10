using Microsoft.JSInterop;

namespace FormCraft.UnitTests.Security;

public class CsrfTokenServiceTests
{
    [Fact]
    public async Task Should_Generate_And_Validate_Token_When_Storage_Is_Available()
    {
        // Arrange
        var jsRuntime = new FakeSessionStorageJsRuntime();
        var service = new BlazorCsrfTokenService(jsRuntime);

        // Act
        var token = await service.GenerateTokenAsync();
        var isValid = await service.ValidateTokenAsync(token);

        // Assert
        token.ShouldNotBeNullOrEmpty();
        isValid.ShouldBeTrue();
        jsRuntime.Storage.ShouldContainKey("FormCraft_CsrfToken");
    }

    [Fact]
    public async Task Should_Reject_Token_That_Was_Not_Issued()
    {
        // Arrange
        var jsRuntime = new FakeSessionStorageJsRuntime();
        var service = new BlazorCsrfTokenService(jsRuntime);

        // Act
        _ = await service.GenerateTokenAsync();
        var isValid = await service.ValidateTokenAsync("forged-token");

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Reject_Null_Or_Empty_Token()
    {
        // Arrange
        var jsRuntime = new FakeSessionStorageJsRuntime();
        var service = new BlazorCsrfTokenService(jsRuntime);

        // Act & Assert
        (await service.ValidateTokenAsync(null!)).ShouldBeFalse();
        (await service.ValidateTokenAsync("")).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Validate_Token_Generated_During_Prerendering_When_Storage_Was_Unavailable()
    {
        // Arrange - simulate prerendering: JS interop fails during generation
        var jsRuntime = new FakeSessionStorageJsRuntime { JsInteropAvailable = false };
        var service = new BlazorCsrfTokenService(jsRuntime);

        var token = await service.GenerateTokenAsync();

        // Interop becomes available once the circuit is interactive
        jsRuntime.JsInteropAvailable = true;

        // Act
        var isValid = await service.ValidateTokenAsync(token);

        // Assert - the token issued during prerendering must still validate (old behavior: always false)
        isValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Persist_Pending_Token_Once_Storage_Becomes_Available()
    {
        // Arrange
        var jsRuntime = new FakeSessionStorageJsRuntime { JsInteropAvailable = false };
        var service = new BlazorCsrfTokenService(jsRuntime);
        var token = await service.GenerateTokenAsync();
        jsRuntime.Storage.ShouldBeEmpty();

        // Act
        jsRuntime.JsInteropAvailable = true;
        _ = await service.ValidateTokenAsync(token);

        // Assert - the token was lazily persisted during validation
        jsRuntime.Storage["FormCraft_CsrfToken"].ShouldBe(token);
    }

    [Fact]
    public async Task Should_Validate_In_Memory_Token_When_Storage_Never_Becomes_Available()
    {
        // Arrange - JS interop unavailable for the entire lifetime (e.g., static prerendering)
        var jsRuntime = new FakeSessionStorageJsRuntime { JsInteropAvailable = false };
        var service = new BlazorCsrfTokenService(jsRuntime);
        var token = await service.GenerateTokenAsync();

        // Act
        var isValid = await service.ValidateTokenAsync(token);
        var isForgedValid = await service.ValidateTokenAsync("forged-token");

        // Assert
        isValid.ShouldBeTrue();
        isForgedValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Reject_Stale_Token_After_A_New_Token_Is_Generated()
    {
        // Arrange
        var jsRuntime = new FakeSessionStorageJsRuntime();
        var service = new BlazorCsrfTokenService(jsRuntime);
        var firstToken = await service.GenerateTokenAsync();

        // Act
        var secondToken = await service.GenerateTokenAsync();

        // Assert
        (await service.ValidateTokenAsync(firstToken)).ShouldBeFalse();
        (await service.ValidateTokenAsync(secondToken)).ShouldBeTrue();
    }

    /// <summary>
    /// Minimal IJSRuntime fake backed by a dictionary that emulates sessionStorage,
    /// with a switch to simulate prerendering (JS interop unavailable).
    /// </summary>
    private sealed class FakeSessionStorageJsRuntime : IJSRuntime
    {
        public Dictionary<string, string> Storage { get; } = new();

        public bool JsInteropAvailable { get; set; } = true;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (!JsInteropAvailable)
            {
                throw new InvalidOperationException(
                    "JavaScript interop calls cannot be issued at this time. This is because the component is being statically rendered.");
            }

            switch (identifier)
            {
                case "sessionStorage.setItem":
                    Storage[(string)args![0]!] = (string)args[1]!;
                    return ValueTask.FromResult(default(TValue)!);
                case "sessionStorage.getItem":
                    Storage.TryGetValue((string)args![0]!, out var value);
                    return ValueTask.FromResult((TValue)(object?)value!);
                default:
                    throw new NotSupportedException($"Unexpected JS interop call: {identifier}");
            }
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);
    }
}
