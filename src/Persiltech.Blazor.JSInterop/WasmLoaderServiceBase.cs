namespace Persiltech.Blazor.JSInterop;

/// <summary>
/// Base class for a service that calls into a WebAssembly module instantiated from JavaScript.
/// </summary>
/// <remarks>
/// On the first call it imports the JavaScript loader module, asks it to instantiate the
/// <c>.wasm</c> file, and keeps the resulting instance. The loader module itself is released
/// right away: only the WebAssembly instance is kept, and it is what the calls go to.
/// </remarks>
public abstract class WasmLoaderServiceBase : JSLoaderServiceBase
{
    private readonly string WasmModulePath;
    private readonly string WasmLoader;

    /// <summary>
    /// Initializes the service with the loader and the WebAssembly module it will call into.
    /// </summary>
    /// <param name="logger">Logger that receives the details of any failed call.</param>
    /// <param name="jsRuntime">The Blazor JSInterop runtime.</param>
    /// <param name="jsModulePath">
    /// Path of the JavaScript module that instantiates the WebAssembly file. The package ships
    /// one at <c>./_content/Persiltech.Blazor.JSInterop/wasmModuleLoader.js</c>.
    /// </param>
    /// <param name="wasmModulePath">Path of the <c>.wasm</c> file to instantiate.</param>
    /// <param name="wasmLoader">
    /// Name of the function exported by the loader module that performs the instantiation.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="logger"/> or <paramref name="jsRuntime"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="jsModulePath"/>, <paramref name="wasmModulePath"/> or
    /// <paramref name="wasmLoader"/> is empty or white space.
    /// </exception>
    protected WasmLoaderServiceBase(
        ILogger logger,
        IJSRuntime jsRuntime,
        string jsModulePath,
        string wasmModulePath,
        string wasmLoader = "wasmLoader") : base(logger, jsRuntime, jsModulePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(wasmModulePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(wasmLoader);

        WasmModulePath = wasmModulePath;
        WasmLoader = wasmLoader;
    }

    /// <summary>
    /// Instantiates the WebAssembly module and routes the calls to it, not to the loader.
    /// </summary>
    /// <param name="jsModule">The JavaScript loader module that was just imported.</param>
    /// <returns>The WebAssembly instance the calls go to.</returns>
    /// <exception cref="InvalidOperationException">
    /// The loader returned no instance for <c>wasmModulePath</c>.
    /// </exception>
    protected override async ValueTask<IJSObjectReference> ResolveTargetAsync(IJSObjectReference jsModule)
    {
        try
        {
            var wasmModuleInstance = await jsModule.InvokeAsync<IJSObjectReference>(WasmLoader, WasmModulePath);

            return wasmModuleInstance ?? throw new InvalidOperationException(
                $"'{WasmLoader}' returned no WebAssembly instance for '{WasmModulePath}'.");
        }
        finally
        {
            // El cargador ya cumplió su función, incluso si la instanciación falló.
            await DisposeReferenceAsync(jsModule);
        }
    }
}
