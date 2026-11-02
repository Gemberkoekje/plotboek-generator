using Microsoft.Extensions.Logging;

namespace PlotboekGenerator.test.TestHelpers;

/// <summary>
/// Captures every formatted log message so a test can assert on what was actually logged,
/// rather than only on a mocked call having happened.
/// </summary>
public sealed class CapturingLogger<T> : ILogger<T>
{
    public List<string> Messages { get; } = [];

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
        => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        => Messages.Add(formatter(state, exception));

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
