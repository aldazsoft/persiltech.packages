namespace Persiltech.Blazor.JSInterop.Tests.Fakes;

/// <summary>
/// Exposes the protected surface of <see cref="JSLoaderServiceBase"/> so it can be exercised.
/// </summary>
internal sealed class ProbeJSService(
    ILogger logger,
    IJSRuntime jsRuntime,
    string jsModulePath) : JSLoaderServiceBase(logger, jsRuntime, jsModulePath)
{
    public Task<T?> CallAsync<T>(string methodName, params object?[]? parameters) =>
        InvokeAsync<T>(methodName, parameters);

    public Task CallVoidAsync(string methodName, params object?[]? parameters) =>
        InvokeVoidAsync(methodName, parameters);
}

/// <summary>
/// Exposes the protected surface of <see cref="WasmLoaderServiceBase"/> so it can be exercised.
/// </summary>
internal sealed class ProbeWasmService(
    ILogger logger,
    IJSRuntime jsRuntime,
    string jsModulePath,
    string wasmModulePath,
    string wasmLoader = "wasmLoader") : WasmLoaderServiceBase(logger, jsRuntime, jsModulePath, wasmModulePath, wasmLoader)
{
    public Task<T?> CallAsync<T>(string methodName, params object?[]? parameters) =>
        InvokeAsync<T>(methodName, parameters);
}
