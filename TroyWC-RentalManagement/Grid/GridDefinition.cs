using System.Linq.Expressions;

namespace TroyWC_RentalManagement.Grid;

/// <summary>A button or link shown on every row of a grid.</summary>
/// <param name="OpensModal">True renders a button that loads <paramref name="Url"/> into the CRUD modal; false renders a plain link.</param>
public sealed record GridAction<T>(
    string Label,
    Func<T, string> Url,
    string CssClass = "btn btn-sm btn-outline-secondary",
    bool OpensModal = true);

/// <summary>Describes how rows of type <typeparamref name="T"/> are shown, sorted and acted on.</summary>
public sealed class GridDefinition<T>
{
    public required IReadOnlyList<GridColumn<T>> Columns { get; init; }

    /// <summary>Unique row key. Used as the final sort so paging is stable.</summary>
    public required Expression<Func<T, int>> Key { get; init; }

    /// <summary>Sort key of the column used when the request has no valid sort.</summary>
    public string? DefaultSort { get; init; }

    public bool DefaultDesc { get; init; }

    /// <summary>Sort keys of columns applied (ascending) after the primary sort, before <see cref="Key"/>.</summary>
    public IReadOnlyList<string> ThenBy { get; init; } = [];

    public IReadOnlyList<GridAction<T>> Actions { get; init; } = [];

    public string EmptyMessage { get; init; } = "No records found.";

    internal GridColumn<T>? FindSortable(string? sortKey) =>
        sortKey is null
            ? null
            : Columns.FirstOrDefault(c => string.Equals(c.SortKey, sortKey, StringComparison.OrdinalIgnoreCase));
}
