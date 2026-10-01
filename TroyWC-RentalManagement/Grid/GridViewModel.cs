using System.Globalization;

namespace TroyWC_RentalManagement.Grid;

public sealed record GridHeader(string Title, string? SortKey, bool IsSorted, bool Desc);

public sealed record GridRowAction(string Label, string Url, string CssClass, bool OpensModal);

public sealed record GridRow(int Key, IReadOnlyList<string> Cells, IReadOnlyList<GridRowAction> Actions);

/// <summary>Rendered by Views/Shared/_Grid.cshtml. Built with <see cref="GridQueryableExtensions.ToGridAsync"/>.</summary>
public sealed class GridViewModel
{
    public required IReadOnlyList<GridHeader> Headers { get; init; }

    public required IReadOnlyList<GridRow> Rows { get; init; }

    public bool HasActions { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public string? Sort { get; init; }

    public bool Desc { get; init; }

    /// <summary>Extra query values (e.g. filters) kept on every sort and page link.</summary>
    public IReadOnlyDictionary<string, string> RouteValues { get; init; } = new Dictionary<string, string>();

    public string EmptyMessage { get; init; } = "No records found.";

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

    public int ColumnCount => Headers.Count + (HasActions ? 1 : 0);

    public int FirstItem => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;

    public int LastItem => Math.Min(Page * PageSize, TotalCount);

    public IDictionary<string, string> PageLink(int page) => LinkValues(page, Sort, Desc);

    /// <summary>Clicking a header sorts ascending, or flips the direction when it is already the sort column.</summary>
    public IDictionary<string, string> SortLink(GridHeader header) =>
        LinkValues(1, header.SortKey, header.IsSorted && !header.Desc);

    private Dictionary<string, string> LinkValues(int page, string? sort, bool desc)
    {
        var values = new Dictionary<string, string>(RouteValues, StringComparer.OrdinalIgnoreCase);

        if (page > 1)
            values["page"] = page.ToString(CultureInfo.InvariantCulture);

        if (PageSize != GridRequest.DefaultPageSize)
            values["pageSize"] = PageSize.ToString(CultureInfo.InvariantCulture);

        if (sort is not null)
        {
            values["sort"] = sort;
            if (desc)
                values["desc"] = "true";
        }

        return values;
    }
}
