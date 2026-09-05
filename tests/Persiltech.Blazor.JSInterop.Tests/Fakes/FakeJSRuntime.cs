namespace Persiltech.Blazor.JSInterop.Tests.Fakes;

/// <summary>
/// Stands in for the Blazor runtime, resolving each <c>import</c> from a scripted queue.
/// </summary>
internal sealed class FakeJSRuntime : IJSRuntime
{
    private readonly Queue<Func<ValueTask<IJSObjectReference>>> ImportResults = new();

    public List<string> ImportedModules { get; } = [];

    public FakeJSRuntime ThenImports(IJSObjectReference module)
    {
        ImportResults.Enqueue(() => ValueTask.FromResult(module));

        return this;
    }

    /// <summary>
    /// Scripts an import that stays in flight until <paramref name="signal"/> completes.
    /// </summary>
    public FakeJSRuntime ThenImportsAfter(Task signal, IJSObjectReference module)
    {
        ImportResults.Enqueue(async () =>
        {
            await signal;

            return module;
        });

        return this;
    }

    public FakeJSRuntime ThenFails(Exception exception)
    {
        ImportResults.Enqueue(() => throw exception);

        return this;
    }

    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        if (identifier != "import")
        {
            throw new InvalidOperationException($"Unexpected root call '{identifier}'.");
        }

        ImportedModules.Add((string)args![0]!);

        if (ImportResults.Count == 0)
        {
            throw new InvalidOperationException($"No import result was scripted for call {ImportedModules.Count}.");
        }

        return (TValue)(object)await ImportResults.Dequeue().Invoke();
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
        InvokeAsync<TValue>(identifier, args);
}
