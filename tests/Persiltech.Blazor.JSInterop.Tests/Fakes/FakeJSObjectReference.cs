namespace Persiltech.Blazor.JSInterop.Tests.Fakes;

/// <summary>
/// Stands in for an imported module or for a WebAssembly instance, recording every call.
/// </summary>
internal sealed class FakeJSObjectReference : IJSObjectReference
{
    public List<(string Identifier, object?[]? Arguments)> Calls { get; } = [];

    public int DisposeCount { get; private set; }

    public Exception? ThrowOnCall { get; set; }

    public Exception? ThrowOnDispose { get; set; }

    public Func<string, object?[]?, object?>? Handler { get; set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        Calls.Add((identifier, args));

        if (ThrowOnCall is not null)
        {
            throw ThrowOnCall;
        }

        var result = Handler?.Invoke(identifier, args);

        return ValueTask.FromResult(result is null ? default! : (TValue)result);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
        InvokeAsync<TValue>(identifier, args);

    public ValueTask DisposeAsync()
    {
        DisposeCount++;

        return ThrowOnDispose is null
            ? ValueTask.CompletedTask
            : ValueTask.FromException(ThrowOnDispose);
    }
}
