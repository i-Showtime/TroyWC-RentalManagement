using System.Globalization;
using System.Linq.Expressions;

namespace TroyWC_RentalManagement.Grid;

/// <summary>
/// A grid column over row type <typeparamref name="T"/>. The same expression is used to
/// sort in the database and, compiled, to produce the cell text.
/// </summary>
public sealed class GridColumn<T>
{
    public required string Header { get; init; }

    /// <summary>Key used in the ?sort= query value; null when the column is not sortable.</summary>
    public string? SortKey { get; init; }

    public LambdaExpression? SortExpression { get; init; }

    public required Func<T, string> Display { get; init; }

    /// <param name="value">Member of the row to show (and sort by when <paramref name="sortable"/>).</param>
    /// <param name="format">Standard format string for <see cref="IFormattable"/> values, e.g. "C".</param>
    /// <param name="display">Custom text for the value; overrides <paramref name="format"/>.</param>
    /// <param name="sortKey">Query value for sorting; defaults to the header in lower case without symbols.</param>
    public static GridColumn<T> For<TValue>(
        Expression<Func<T, TValue>> value,
        string header,
        bool sortable = true,
        string? format = null,
        Func<TValue, string>? display = null,
        string? sortKey = null)
    {
        var compiled = value.Compile();

        return new GridColumn<T>
        {
            Header = header,
            SortKey = sortable ? sortKey ?? ToSortKey(header) : null,
            SortExpression = sortable ? value : null,
            Display = display is not null
                ? row => display(compiled(row))
                : row => FormatValue(compiled(row), format),
        };
    }

    private static string FormatValue(object? value, string? format) => value switch
    {
        null => string.Empty,
        IFormattable formattable when format is not null => formattable.ToString(format, CultureInfo.CurrentCulture),
        _ => value.ToString() ?? string.Empty,
    };

    private static string ToSortKey(string header) =>
        new(header.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
