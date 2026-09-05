namespace Persiltech.Blazor.JSInterop.Tests;

public class WasmLoaderServiceBaseTests
{
    private const string LoaderPath = "./_content/Persiltech.Blazor.JSInterop/wasmModuleLoader.js";
    private const string WasmPath = "./wasm/add.wasm";

    [Fact]
    public async Task TheCallsGoToTheInstanceAndTheLoaderIsReleased()
    {
        var wasmInstance = new FakeJSObjectReference { Handler = (_, _) => 3 };
        var loader = new FakeJSObjectReference { Handler = (_, _) => wasmInstance };
        var jsRuntime = new FakeJSRuntime().ThenImports(loader);

        await using var service = new ProbeWasmService(NullLogger.Instance, jsRuntime, LoaderPath, WasmPath);

        Assert.Equal(3, await service.CallAsync<int>("add", 1, 2));

        Assert.Equal("wasmLoader", Assert.Single(loader.Calls).Identifier);
        Assert.Equal(new object?[] { WasmPath }, loader.Calls[0].Arguments);
        Assert.Equal(1, loader.DisposeCount);
        Assert.Equal(0, wasmInstance.DisposeCount);
        Assert.Equal(new[] { "add" }, wasmInstance.Calls.Select(c => c.Identifier));
    }

    [Fact]
    public async Task TheLoaderNameIsConfigurable()
    {
        var wasmInstance = new FakeJSObjectReference();
        var loader = new FakeJSObjectReference { Handler = (_, _) => wasmInstance };
        var jsRuntime = new FakeJSRuntime().ThenImports(loader);

        await using var service = new ProbeWasmService(NullLogger.Instance, jsRuntime, LoaderPath, WasmPath, "instantiate");

        await service.CallAsync<int>("add");

        Assert.Equal("instantiate", loader.Calls[0].Identifier);
    }

    [Fact]
    public async Task TheLoaderIsReleasedEvenWhenInstantiationFails()
    {
        var loader = new FakeJSObjectReference { ThrowOnCall = new JSException("bad wasm") };
        var jsRuntime = new FakeJSRuntime().ThenImports(loader);

        await using var service = new ProbeWasmService(NullLogger.Instance, jsRuntime, LoaderPath, WasmPath);

        Assert.Equal(default, await service.CallAsync<int>("add", 1, 2));
        Assert.Equal(1, loader.DisposeCount);
    }

    [Fact]
    public async Task AnInstanceThatNeverMaterializesDoesNotBecomeANullReference()
    {
        var loader = new FakeJSObjectReference { Handler = (_, _) => null };
        var wasmInstance = new FakeJSObjectReference { Handler = (_, _) => 5 };
        var secondLoader = new FakeJSObjectReference { Handler = (_, _) => wasmInstance };
        var jsRuntime = new FakeJSRuntime().ThenImports(loader).ThenImports(secondLoader);

        await using var service = new ProbeWasmService(NullLogger.Instance, jsRuntime, LoaderPath, WasmPath);

        Assert.Equal(default, await service.CallAsync<int>("add"));
        Assert.Equal(5, await service.CallAsync<int>("add"));
    }

    [Fact]
    public async Task DisposeAsyncReleasesAnInstanceThatArrivesAfterIt()
    {
        var wasmInstance = new FakeJSObjectReference();
        var loader = new FakeJSObjectReference { Handler = (_, _) => wasmInstance };
        var importCompleted = new TaskCompletionSource();
        var jsRuntime = new FakeJSRuntime().ThenImportsAfter(importCompleted.Task, loader);

        var service = new ProbeWasmService(NullLogger.Instance, jsRuntime, LoaderPath, WasmPath);

        var call = service.CallAsync<int>("add", 1, 2);

        await service.DisposeAsync();

        importCompleted.SetResult();

        Assert.Equal(default, await call);
        Assert.Equal(1, loader.DisposeCount);
        Assert.Equal(1, wasmInstance.DisposeCount);
    }

    [Fact]
    public void ConstructorRejectsAnEmptyWasmPath()
    {
        var jsRuntime = new FakeJSRuntime();

        Assert.Throws<ArgumentException>(
            () => new ProbeWasmService(NullLogger.Instance, jsRuntime, LoaderPath, "  "));
    }
}
