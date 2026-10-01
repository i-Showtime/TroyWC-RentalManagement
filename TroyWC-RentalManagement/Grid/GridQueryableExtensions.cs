using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace TroyWC_RentalManagement.Grid;

public static class GridQueryableExtensions
{
    /// <summary>
    /// Sorts and pages <paramref name="source"/> in the database and builds the grid view model.
    /// Project to a row class with member-init syntax (new Row { ... }) first so EF can sort on its members.
    /// </summary>
    /// <param name="routeValues">Extra query values (e.g. filters) to keep on sort and page links.</param>
    public static async Task<GridViewModel> ToGridAsync<T>(
        this IQueryable<T> source,
        GridDefinition<T> definition,
        GridRequest request,
        IReadOnlyDictionary<string, string>? routeValues = null,
        CancellationToken cancellationToken = default)
    {
        var sortColumn = definition.FindSortable(request.Sort);
        var desc = request.Desc;
        if (sortColumn is null)
        {
            sortColumn = definition.FindSortable(definition.DefaultSort);
            desc = definition.DefaultDesc;
        }

        IOrderedQueryable<T>? ordered = null;
        if (sortColumn is not null)
            ordered = ApplyOrder(source, null, sortColumn.SortExpression!, desc);

        foreach (var thenByKey in definition.ThenBy)
        {
            var column = definition.FindSortable(thenByKey);
            if (column is not null && column != sortColumn)
                ordered = ApplyOrder(source, ordered, column.SortExpression!, desc: false);
        }

        ordered = ApplyOrder(source, ordered, definition.Key, desc: false);

        var totalCount = await source.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)request.PageSize));
        var page = Math.Min(request.Page, totalPages);

        var items = await ordered
            .Skip((page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var rowKey = definition.Key.Compile();

        return new GridViewModel
        {
            Headers = definition.Columns
                .Select(c => new GridHeader(c.Header, c.SortKey, c == sortColumn, c == sortColumn && desc))
                .ToList(),
            Rows = items
                .Select(item => new GridRow(
                    rowKey(item),
                    definition.Columns.Select(c => c.Display(item)).ToList(),
                    definition.Actions
                        .Select(a => new GridRowAction(a.Label, a.Url(item), a.CssClass, a.OpensModal))
                        .ToList()))
                .ToList(),
            HasActions = definition.Actions.Count > 0,
            Page = page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            Sort = sortColumn?.SortKey,
            Desc = desc,
            RouteValues = routeValues ?? new Dictionary<string, string>(),
            EmptyMessage = definition.EmptyMessage,
        };
    }

    /// <summary>Calls OrderBy/ThenBy (or the Descending variants) with a key whose type is only known at runtime.</summary>
    private static IOrderedQueryable<T> ApplyOrder<T>(
        IQueryable<T> source, IOrderedQueryable<T>? ordered, LambdaExpression keySelector, bool desc)
    {
        var method = (ordered is null, desc) switch
        {
            (true, false) => nameof(Queryable.OrderBy),
            (true, true) => nameof(Queryable.OrderByDescending),
            (false, false) => nameof(Queryable.ThenBy),
            (false, true) => nameof(Queryable.ThenByDescending),
        };

        var current = ordered ?? source;
        var call = Expression.Call(
            typeof(Queryable),
            method,
            [typeof(T), keySelector.ReturnType],
            current.Expression,
            Expression.Quote(keySelector));

        return (IOrderedQueryable<T>)current.Provider.CreateQuery<T>(call);
    }
}
