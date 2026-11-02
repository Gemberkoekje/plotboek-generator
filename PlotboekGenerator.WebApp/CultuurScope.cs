using System.Globalization;

namespace PlotboekGenerator.WebApp;

/// <summary>Runs a piece of work in another language than the request's.</summary>
public static class CultuurScope
{
    /// <summary>
    /// Runs <paramref name="work"/> with <paramref name="culture"/> as the language of the text. The
    /// caller's language is untouched afterwards: a culture set inside an async method does not flow
    /// back out of it.
    /// </summary>
    /// <typeparam name="T">What the work returns.</typeparam>
    /// <param name="culture">The language to run in; <c>null</c> keeps the current one.</param>
    /// <param name="work">The work.</param>
    /// <returns>What the work returned.</returns>
    public static async Task<T> InAsync<T>(CultureInfo? culture, Func<Task<T>> work)
    {
        ArgumentNullException.ThrowIfNull(work);

        if (culture is not null)
        {
            CultureInfo.CurrentUICulture = culture;
        }

        return await work();
    }
}
