namespace Persiltech.Blazor.JSInterop.Tests;

public class JSLoaderServiceBaseTests
{
    private const string ModulePath = "./js/module.js";

    [Fact]
    public void ConstructorRejectsAnEmptyModulePath()
    {
        var jsRuntime = new FakeJSRuntime();

        Assert.Throws<ArgumentException>(() => new ProbeJSService(NullLogger.Instance, jsRuntime, "  "));
    }

    [Fact]
    public void ConstructingTheServiceImportsNothing()
    {
        var jsRuntime = new FakeJSRuntime();

        _ = new ProbeJSService(NullLogger.Instance, jsRuntime, ModulePath);

        Assert.Empty(jsRuntime.ImportedModules);
    }

    [Fact]
    public async Task TheModuleIsImportedOnceAndReused()
    {
        var module = new FakeJSObjectReference { Handler = (_, _) => 7 };
        var jsRuntime = new FakeJSRuntime().ThenImports(module);

        await using var service = new ProbeJSService(NullLogger.Instance, jsRuntime, ModulePath);

        await service.CallAsync<int>("first");
        await service.CallAsync<int>("second");

        Assert.Equal(new[] { ModulePath }, jsRuntime.ImportedModules);
        Assert.Equal(new[] { "first", "second" }, module.Calls.Select(c => c.Identifier));
    }

    [Fact]
    public async Task TheArgumentsReachTheModuleUntouched()
    {
        var module = new FakeJSObjectReference();
        var jsRuntime = new FakeJSRuntime().ThenImports(module);

        await using var service = new ProbeJSService(NullLogger.Instance, jsRuntime, ModulePath);

        await service.CallVoidAsync("withArguments", 1, "two", null);
        await service.CallVoidAsync("withoutArguments");

        Assert.Equal(new object?[] { 1, "two", null }, module.Calls[0].Arguments);
        Assert.Empty(module.Calls[1].Arguments!);
    }

    [Fact]
    public async Task AFailedImportIsLoggedAndNotThrown()
    {
        var jsRuntime = new FakeJSRuntime().ThenFails(new JSException("404"));

        await using var service = new ProbeJSService(NullLogger.Instance, jsRuntime, ModulePath);

        Assert.Equal(default, await service.CallAsync<int>("anything"));
    }

    [Fact]
    public async Task AFailedImportIsNotCachedAndTheNextCallRetries()
    {
        var module = new FakeJSObjectReference { Handler = (_, _) => 42 };
        var jsRuntime = new FakeJSRuntime()
            .ThenFails(new InvalidOperationException("JavaScript interop calls cannot be issued at this time."))
            .ThenImports(module);

        await using var service = new ProbeJSService(NullLogger.Instance, jsRuntime, ModulePath);

        Assert.Equal(default, await service.CallAsync<int>("first"));
        Assert.Equal(42, await service.CallAsync<int>("second"));
        Assert.Equal(2, jsRuntime.ImportedModules.Count);
    }

    [Fact]
    public async Task AFailedCallIsLoggedAndNotThrown()
    {
        var module = new FakeJSObjectReference { ThrowOnCall = new JSException("boom") };
        var jsRuntime = new FakeJSRuntime().ThenImports(module);

        await using var service = new ProbeJSService(NullLogger.Instance, jsRuntime, ModulePath);

        Assert.Equal(default, await service.CallAsync<int>("broken"));
        await service.CallVoidAsync("broken");
    }

    [Fact]
    public async Task DisposeAsyncReleasesNothingWhenNoCallWasMade()
    {
        var module = new FakeJSObjectReference();
        var jsRuntime = new FakeJSRuntime().ThenImports(module);

        var service = new ProbeJSService(NullLogger.Instance, jsRuntime, ModulePath);
        await service.DisposeAsync();

        Assert.Empty(jsRuntime.ImportedModules);
        Assert.Equal(0, module.DisposeCount);
    }

    [Fact]
    public async Task DisposeAsyncReleasesTheModuleOnlyOnce()
    {
        var module = new FakeJSObjectReference();
        var jsRuntime = new FakeJSRuntime().ThenImports(module);

        var service = new ProbeJSService(NullLogger.Instance, jsRuntime, ModulePath);
        await service.CallVoidAsync("anything");

        await service.DisposeAsync();
        await service.DisposeAsync();

        Assert.Equal(1, module.DisposeCount);
    }

    [Fact]
    public async Task DisposeAsyncReleasesAModuleThatArrivesAfterIt()
    {
        var module = new FakeJSObjectReference();
        var importCompleted = new TaskCompletionSource();
        var jsRuntime = new FakeJSRuntime().ThenImportsAfter(importCompleted.Task, module);

        var service = new ProbeJSService(NullLogger.Instance, jsRuntime, ModulePath);

        var call = service.CallVoidAsync("anything");

        await service.DisposeAsync();

        importCompleted.SetResult();

        await call;

        Assert.Equal(1, module.DisposeCount);
        Assert.Empty(module.Calls);
    }

    [Fact]
    public async Task ACallOnADisposedServiceIsSkippedAndNotReportedAsAnError()
    {
        var module = new FakeJSObjectReference();
        var jsRuntime = new FakeJSRuntime().ThenImports(module);
        var logger = new RecordingLogger();

        var service = new ProbeJSService(logger, jsRuntime, ModulePath);
        await service.CallVoidAsync("anything");
        await service.DisposeAsync();

        Assert.Equal(default, await service.CallAsync<int>("afterDispose"));

        var entry = Assert.Single(logger.Entries);

        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.IsType<ObjectDisposedException>(entry.Exception);
    }

    [Fact]
    public async Task AFailedCallProducesOneLogEntryCarryingTheException()
    {
        var failure = new JSException("boom");
        var module = new FakeJSObjectReference { ThrowOnCall = failure };
        var jsRuntime = new FakeJSRuntime().ThenImports(module);
        var logger = new RecordingLogger();

        await using var service = new ProbeJSService(logger, jsRuntime, ModulePath);

        await service.CallVoidAsync("broken");

        var entry = Assert.Single(logger.Entries);

        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(failure, entry.Exception);
    }

    [Fact]
    public async Task DisposeAsyncToleratesATornDownCircuit()
    {
        var module = new FakeJSObjectReference { ThrowOnDispose = new JSDisconnectedException("gone") };
        var jsRuntime = new FakeJSRuntime().ThenImports(module);

        var service = new ProbeJSService(NullLogger.Instance, jsRuntime, ModulePath);
        await service.CallVoidAsync("anything");

        await service.DisposeAsync();

        Assert.Equal(1, module.DisposeCount);
    }
}
