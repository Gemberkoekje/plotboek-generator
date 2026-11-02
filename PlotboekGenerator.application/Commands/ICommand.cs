namespace PlotboekGenerator.Application.Commands;

/// <summary>
/// Marker interface for command objects.
/// </summary>
public interface ICommand
{
}

/// <summary>
/// Marker interface for command objects that return a result.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the command</typeparam>
public interface ICommand<TResult>
{
}
