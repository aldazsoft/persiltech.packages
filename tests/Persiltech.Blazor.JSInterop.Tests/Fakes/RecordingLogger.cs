namespace Persiltech.Blazor.JSInterop.Tests.Fakes;

/// <summary>
/// Records every entry the service writes, so its level and its exception can be asserted.
/// </summary>
internal sealed class RecordingLogger : ILogger
{
    public List<(LogLevel Level, Exception? Exception)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, exception));
}
