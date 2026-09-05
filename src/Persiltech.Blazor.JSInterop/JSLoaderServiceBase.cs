namespace Persiltech.Blazor.JSInterop;

/// <summary>
/// Base class for a service that calls into a JavaScript module loaded through JSInterop.
/// </summary>
/// <remarks>
/// The module is imported lazily, on the first call, and the same reference is reused
/// afterwards. Derive from this class, pass the module path to the constructor, and expose
/// your own methods on top of <see cref="InvokeAsync{T}"/> and
/// <see cref="InvokeVoidAsync"/>.
/// </remarks>
public abstract partial class JSLoaderServiceBase : IAsyncDisposable
{
    private readonly SemaphoreSlim ImportGate = new(1, 1);
    private readonly ILogger Logger;
    private readonly IJSRuntime JSRuntime;
    private readonly string JSModulePath;

    private IJSObjectReference? TargetReference;
    private bool IsDisposed;

    /// <summary>
    /// Initializes the service with the module it will call into.
    /// </summary>
    /// <param name="logger">Logger that receives the details of any failed call.</param>
    /// <param name="jsRuntime">The Blazor JSInterop runtime.</param>
    /// <param name="jsModulePath">
    /// Path of the JavaScript module to import, as the browser resolves it
    /// (Ej. <c>./js/geolocationModule.js</c> or <c>./_content/{package}/module.js</c>).
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="logger"/> or <paramref name="jsRuntime"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="jsModulePath"/> is empty or white space.
    /// </exception>
    protected JSLoaderServiceBase(
        ILogger logger,
        IJSRuntime jsRuntime,
        string jsModulePath)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(jsRuntime);
        ArgumentException.ThrowIfNullOrWhiteSpace(jsModulePath);

        Logger = logger;
        JSRuntime = jsRuntime;
        JSModulePath = jsModulePath;
    }

    /// <summary>
    /// Resolves the reference the calls are routed to, from the module just imported.
    /// </summary>
    /// <remarks>
    /// The default implementation routes the calls to the module itself. Override it to call
    /// into something the module produces — as <see cref="WasmLoaderServiceBase"/> does with
    /// the WebAssembly instance — and take ownership of <paramref name="jsModule"/>: whatever
    /// this method does not return is never released by the base class.
    /// </remarks>
    /// <param name="jsModule">The module that was just imported.</param>
    /// <returns>The reference <see cref="InvokeAsync{T}"/> and <see cref="InvokeVoidAsync"/> call into.</returns>
    protected virtual ValueTask<IJSObjectReference> ResolveTargetAsync(IJSObjectReference jsModule) =>
        ValueTask.FromResult(jsModule);

    private async Task<IJSObjectReference> GetTargetAsync()
    {
        if (TargetReference is not null)
        {
            return TargetReference;
        }

        await ImportGate.WaitAsync();

        try
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (TargetReference is null)
            {
                // Una importación fallida no se cachea: la referencia sigue nula y la
                // siguiente llamada vuelve a intentarlo. Es lo que salva al servicio de
                // quedar muerto el resto del scope cuando el primer intento cae en el
                // prerender de Blazor Server.
                var target = await ResolveTargetAsync(
                    await JSRuntime.InvokeAsync<IJSObjectReference>("import", JSModulePath));

                if (IsDisposed)
                {
                    // El servicio se liberó mientras la importación seguía en vuelo, así que
                    // DisposeAsync no encontró nada que liberar. Cachearla aquí la dejaría
                    // sin dueño: la referencia se libera ahora o no la libera nadie.
                    await DisposeReferenceAsync(target);

                    throw new ObjectDisposedException(GetType().FullName);
                }

                TargetReference = target;
            }

            return TargetReference;
        }
        finally
        {
            ImportGate.Release();
        }
    }

    /// <summary>
    /// Calls a function of the module and returns its result.
    /// </summary>
    /// <remarks>
    /// A failure is <strong>logged, not thrown</strong>: the method returns
    /// <c>default(T)</c>. Check the result before using it, and read the log to find out what
    /// happened.
    /// </remarks>
    /// <typeparam name="T">Type the function's result is deserialized into.</typeparam>
    /// <param name="methodName">Name of the exported function to call.</param>
    /// <param name="parameters">Arguments to pass, if any.</param>
    /// <returns>The result of the call, or <c>default(T)</c> if it failed.</returns>
    protected async Task<T?> InvokeAsync<T>(string methodName, params object?[]? parameters)
    {
        try
        {
            var target = await GetTargetAsync();

            return await target.InvokeAsync<T>(methodName, parameters);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or ObjectDisposedException)
        {
            LogCallSkipped(Logger, ex, JSModulePath, methodName);
        }
        catch (Exception ex)
        {
            LogCallFailed(Logger, ex, JSModulePath, methodName);
        }

        return default;
    }

    /// <summary>
    /// Calls a function of the module that returns nothing.
    /// </summary>
    /// <remarks>
    /// As with <see cref="InvokeAsync{T}"/>, a failure is <strong>logged, not thrown</strong>,
    /// so the call returns as if it had succeeded.
    /// </remarks>
    /// <param name="methodName">Name of the exported function to call.</param>
    /// <param name="parameters">Arguments to pass, if any.</param>
    protected async Task InvokeVoidAsync(string methodName, params object?[]? parameters)
    {
        try
        {
            var target = await GetTargetAsync();

            await target.InvokeVoidAsync(methodName, parameters);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or ObjectDisposedException)
        {
            LogCallSkipped(Logger, ex, JSModulePath, methodName);
        }
        catch (Exception ex)
        {
            LogCallFailed(Logger, ex, JSModulePath, methodName);
        }
    }

    /// <summary>
    /// Releases a JSInterop reference without letting a torn-down circuit surface as an error.
    /// </summary>
    /// <param name="reference">The reference to release.</param>
    private protected async ValueTask DisposeReferenceAsync(IJSObjectReference reference)
    {
        try
        {
            await reference.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // El circuito ya no existe: el navegador se llevó la referencia consigo.
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            LogDisposeFailed(Logger, ex, JSModulePath);
        }
    }

    /// <summary>
    /// Releases the JavaScript module, if it was ever imported.
    /// </summary>
    /// <remarks>
    /// Nothing is released when no call was made, because the module is imported lazily. A
    /// call still in flight is not waited for: the reference it is importing is released as
    /// soon as it arrives, and the call itself returns as a skipped one.
    /// Override it to release your own resources, and call the base implementation.
    /// </remarks>
    public virtual async ValueTask DisposeAsync()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;

        var target = TargetReference;
        TargetReference = null;

        if (target is not null)
        {
            await DisposeReferenceAsync(target);
        }

        // El semáforo no se libera a propósito. Nadie pide su AvailableWaitHandle, así que
        // Dispose() no tendría nada que liberar, y hacerlo aquí reventaría el Release() de una
        // importación que siguiera en vuelo. De esa referencia se encarga GetTargetAsync.

        GC.SuppressFinalize(this);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "JSInterop call failed. Module: {ModulePath}, Method: {MethodName}")]
    private static partial void LogCallFailed(ILogger logger, Exception exception, string modulePath, string methodName);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "JSInterop call skipped, the service or the circuit is gone. Module: {ModulePath}, Method: {MethodName}")]
    private static partial void LogCallSkipped(ILogger logger, Exception exception, string modulePath, string methodName);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Releasing the JSInterop reference failed. Module: {ModulePath}")]
    private static partial void LogDisposeFailed(ILogger logger, Exception exception, string modulePath);
}
