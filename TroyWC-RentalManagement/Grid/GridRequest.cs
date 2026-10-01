namespace TroyWC_RentalManagement.Grid;

/// <summary>
/// Paging and sorting state for a grid, bound from the query string
/// (?page=2&amp;pageSize=25&amp;sort=name&amp;desc=true).
/// </summary>
public class GridRequest
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    public int Page
    {
        get => _page;
        set => _page = Math.Max(1, value);
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, MaxPageSize);
    }

    public string? Sort { get; set; }

    public bool Desc { get; set; }
}
