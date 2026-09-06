# Persiltech.Blazor.JSInterop

[![NuGet](https://img.shields.io/nuget/v/Persiltech.Blazor.JSInterop.svg)](https://www.nuget.org/packages/Persiltech.Blazor.JSInterop/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://aldazsoft.github.io/license/)
[![Sponsor](https://img.shields.io/badge/Sponsor-GitHub-ea4aaa.svg)](https://github.com/sponsors/aldazsoft)

Base classes for the Blazor services that call into a JavaScript module or a WebAssembly
module. They import the module **once and lazily**, expose typed calls on top of it, and
release it with the component.

## Installation

    dotnet add package Persiltech.Blazor.JSInterop

## The contract

```csharp
namespace Persiltech.Blazor.JSInterop;

public abstract class JSLoaderServiceBase : IAsyncDisposable
{
    protected JSLoaderServiceBase(ILogger logger, IJSRuntime jsRuntime, string jsModulePath);

    protected Task<T?> InvokeAsync<T>(string methodName, params object?[]? parameters);
    protected Task InvokeVoidAsync(string methodName, params object?[]? parameters);

    protected virtual ValueTask<IJSObjectReference> ResolveTargetAsync(IJSObjectReference jsModule);

    public virtual ValueTask DisposeAsync();
}

public abstract class WasmLoaderServiceBase : JSLoaderServiceBase
{
    protected WasmLoaderServiceBase(
        ILogger logger,
        IJSRuntime jsRuntime,
        string jsModulePath,
        string wasmModulePath,
        string wasmLoader = "wasmLoader");

    protected override ValueTask<IJSObjectReference> ResolveTargetAsync(IJSObjectReference jsModule);
}
```

The module is imported on the **first call**, not when the service is constructed, and the
same reference is reused afterwards. A service that is never used downloads nothing, and
`DisposeAsync` then has nothing to release.

> **Failures are logged, not thrown.** When a call into the module fails, the exception is
> written to the logger and `InvokeAsync<T>` returns `default(T)`; `InvokeVoidAsync` returns as
> if it had succeeded. Check the result before using it.

A **failed import is not cached**: the reference stays empty and the next call tries again. It
matters when the first call lands somewhere the browser is not reachable yet — Blazor Server's
prerender, most often — because the service would otherwise stay dead for the rest of the
scope. And `DisposeAsync` tolerates a circuit that is already gone, so releasing the service
at the end of a Blazor Server circuit never surfaces as an error.

`DisposeAsync` does not wait for a call that is still in flight — a component torn down while
the module is still being imported returns at once. The reference that call is importing is
released as soon as it arrives, and the call itself finishes as a skipped one: it returns the
default value and logs a `Debug` entry, not an error.

## Usage

### A JavaScript module

Write the module and export what you want to call. This one resolves the user's coordinates:

```js
const getPosition = () => {
    return new Promise((resolve, reject) => {
        if ("geolocation" in navigator) {
            navigator.geolocation.getCurrentPosition(returnPosition, returnError);
        }

        function returnPosition(position) {
            resolve({
                latitude: position.coords.latitude,
                longitude: position.coords.longitude
            });
        }

        function returnError(error) {
            reject(error.message);
        }
    });
}

export { getPosition };
```

Then derive a service from `JSLoaderServiceBase`, passing the path of the module. Everything
else is your own API on top of `InvokeAsync`:

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Persiltech.Blazor.JSInterop;

public record struct GeolocationLatLong(double Latitude, double Longitude);

public class GeolocationService(
    ILogger<GeolocationService> logger,
    IJSRuntime jsRuntime) : JSLoaderServiceBase(
        logger,
        jsRuntime,
        "./js/geolocationModule.js")
{
    public async ValueTask<GeolocationLatLong> GetPosition() =>
        await InvokeAsync<GeolocationLatLong>("getPosition");
}
```

`ILogger<T>` is covariant, so you can declare the logger of your own type and pass it straight
to the base class.

Register it:

```csharp
builder.Services.AddScoped<GeolocationService>();
```

And use it from a component:

```razor
@page "/get-position"
@inject GeolocationService Geolocation

<button class="btn btn-primary mb-2" @onclick="ShowPosition">Show my position</button>
<textarea class="form-control" rows="3" disabled @bind="message"></textarea>

@code {
    private string message = string.Empty;

    private async Task ShowPosition()
    {
        var coords = await Geolocation.GetPosition();
        message = $"Latitude: {coords.Latitude}, Longitude: {coords.Longitude}";
    }
}
```

### A WebAssembly module

`WasmLoaderServiceBase` takes two paths: the JavaScript loader that instantiates the module,
and the `.wasm` file itself. The package ships a loader you can use as it is, at
`./_content/Persiltech.Blazor.JSInterop/wasmModuleLoader.js`:

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Persiltech.Blazor.JSInterop;

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
```

Once the WebAssembly module is instantiated, the loader module is released: only the instance
is kept, and it is what the calls go to.

If your loader exports the instantiation function under a different name, pass it as the last
argument — it defaults to `wasmLoader`.

## Design decisions

- The module is imported **lazily**, on the first call, and reused afterwards.
- A **failed import is not cached**, so the next call retries instead of leaving the service dead.
- `DisposeAsync` releases nothing when no call was ever made, and never fails because the
  circuit is gone. It does not block on a call in flight either: the reference that call is
  importing is released when it arrives, so nothing is leaked and nothing is waited for.
- `WasmLoaderServiceBase` releases the JavaScript loader as soon as it has the WebAssembly
  instance — and also when the instantiation fails.
- JSInterop failures are logged and swallowed, so a broken call returns a default value rather
  than throwing. Each failure is **one** log entry, carrying the exception, the module and the
  method that was called.
- Override `ResolveTargetAsync` to route the calls somewhere other than the module itself:
  it is how `WasmLoaderServiceBase` points them at the WebAssembly instance.

### Out of scope

- Dependency injection registration: each derived service decides its own lifetime.
- Writing the JavaScript module or compiling the `.wasm`. Both belong to the consumer.

## Compatibility

`net10.0`, for Blazor applications.

## Version history

The source lives in the [monorepo](https://github.com/aldazsoft/persiltech.packages); this table summarises what each published version changed.

| Version | Changes |
| ------- | ------- |
| 1.1.2   | The `.nuspec` now declares the repository, which is public, and SourceLink is on, so consumers can step into the source while debugging. Support moves to GitHub issues. `Microsoft.JSInterop` and `Microsoft.Extensions.Logging.Abstractions` move from 10.0.9 to 10.0.11. This table no longer lists a 1.0.2 that was never published: those changes shipped in 1.1.0. **No change to the public surface.** |
| 1.1.1   | Fixes a leak when the service is released while its first call is still importing the module — a component torn down while a JavaScript call is pending, which is what a geolocation prompt does. The reference arrived after `DisposeAsync` had already run, so it was cached on a disposed service and nobody ever released it; releasing the internal gate on the way out then failed and was reported as an error the consumer could do nothing about. The reference is now released as it arrives, and a call that outlives its service is logged as `Debug` — it is teardown, not a failure. **No change to the public surface.** The README's examples are corrected: both were missing `using Microsoft.Extensions.Logging;`, so they did not compile as printed. The constructors now document the exceptions they throw. |
| 1.1.0   | Fixes the packaged `wasmModuleLoader.js`, which shipped **truncated** and could not be imported: anyone following the WebAssembly example got a syntax error. A failed import is no longer cached, so the next call retries instead of leaving the service dead for the rest of the scope; and an import that fails is now logged and swallowed, as the contract always promised — before, it was thrown. `DisposeAsync` tolerates a torn-down circuit. Each failure is one structured log entry, with the exception and the name of the method that was called. `WasmLoaderServiceBase` is now `abstract`, derives from `JSLoaderServiceBase`, and takes a plain `ILogger`. The dependency narrows from `Microsoft.AspNetCore.Components.Web` to `Microsoft.JSInterop` and `Microsoft.Extensions.Logging.Abstractions`. **The contract of the calls does not change**: failures are still logged and swallowed. The project website now points to the portfolio page where the package is documented; the real licence text ships inside the `.nupkg` instead of an SPDX expression; the public surface is documented with XML comments; and the README examples are corrected, since they named a namespace that does not exist and omitted the logger the constructors require. |
| 1.0.0 – 1.0.1 | Initial releases of `JSLoaderServiceBase` and `WasmLoaderServiceBase`. |

The contract of the calls has not changed since `1.0.0`: a failure is still logged and
swallowed. `1.1.0` reshapes `WasmLoaderServiceBase` — `abstract`, derived from
`JSLoaderServiceBase`, taking a plain `ILogger` — which a derived service absorbs by
recompiling. Updating is safe; a service deriving from `WasmLoaderServiceBase` needs a rebuild.
`1.1.1` leaves the surface untouched, so it is a drop-in replacement.

## Support

For questions, bug reports or feature requests open an [issue](https://github.com/aldazsoft/persiltech.packages/issues).
You can also see the [package page](https://aldazsoft.github.io/Blazor.JSInterop/).

## Support the development

If this package saves you work, you can support its maintenance on
[GitHub Sponsors](https://github.com/sponsors/aldazsoft).
