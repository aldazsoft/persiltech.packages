namespace Persiltech.Blazor.JSInterop.Sample.Services;

public class AddWasmService(
    ILogger<AddWasmService> logger,
    IJSRuntime jsRuntime) : WasmLoaderServiceBase(
        logger,
        jsRuntime,
        "./_content/Persiltech.Blazor.JSInterop/wasmModuleLoader.js",
        "./wasm/add.wasm")
{
    public async Task<int> Add(int firstAddend, int secondAddend) =>
        await InvokeAsync<int>("add", firstAddend, secondAddend);
}
