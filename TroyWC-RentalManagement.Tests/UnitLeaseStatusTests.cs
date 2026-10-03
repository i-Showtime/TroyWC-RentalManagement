using TroyWC_RentalManagement.Models;
using TroyWC_RentalManagement.Tests.Infrastructure;

namespace TroyWC_RentalManagement.Tests;

/// <summary>The Status column on the property managers' Units grid.</summary>
[Collection(AppCollection.Name)]
public sealed class UnitLeaseStatusTests(RentalAppFactory factory)
{
    private const string ManageUnits = "/Manage/Units?pageSize=100";

    private readonly TestData _data = new(factory);

    [Fact]
    public async Task Units_grid_shows_which_units_are_leased()
    {
        var available = await _data.CreateUnitAsync();
        var leased = await _data.CreateUnitAsync();
        var previouslyLeased = await _data.CreateUnitAsync();
        await _data.CreateLeaseAsync(leased, LeaseStatus.Active);
        await _data.CreateLeaseAsync(previouslyLeased, LeaseStatus.Inactive);
        var (_, manager) = await _data.SignInNewAsync(Roles.PropertyManager);

        var page = await manager.GetAsync(ManageUnits);

        Assert.Equal("Available", StatusOf(page, available));
        Assert.Equal("Leased", StatusOf(page, leased));
        Assert.Equal("Available", StatusOf(page, previouslyLeased));
    }

    [Fact]
    public async Task Units_grid_can_be_sorted_by_status()
    {
        await _data.CreateUnitAsync();
        await _data.CreateLeaseAsync(await _data.CreateUnitAsync());
        var (_, manager) = await _data.SignInNewAsync(Roles.PropertyManager);

        var page = await manager.GetAsync($"{ManageUnits}&sort=status&desc=true");
        var statuses = page.FindAll("tbody tr[data-key]").Select(row => Cell(row, StatusColumn(page))).ToList();

        Assert.Contains("Leased", statuses);
        Assert.Contains("Available", statuses);
        Assert.Equal(statuses.OrderBy(s => s == "Leased" ? 0 : 1), statuses);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(Roles.Applicant)]
    [InlineData(Roles.PropertyManager)]
    public async Task Home_page_has_no_status_column(string? role)
    {
        await _data.CreateUnitAsync();
        var client = role is null ? factory.CreateAppClient() : (await _data.SignInNewAsync(role)).Client;

        var page = await client.GetAsync(Urls.Home);

        Assert.DoesNotContain("Status", Headers(page));
    }

    private static string StatusOf(HtmlPage page, int unitId)
    {
        var row = page.Find($"tr[data-key=\"{unitId}\"]");
        Assert.NotNull(row);
        return Cell(row, StatusColumn(page));
    }

    private static int StatusColumn(HtmlPage page)
    {
        var index = Headers(page).IndexOf("Status");
        Assert.True(index >= 0, "The grid has no Status column.");
        return index;
    }

    private static List<string> Headers(HtmlPage page) =>
        page.FindAll("thead th").Select(th => th.TextContent.Replace("▲", "").Replace("▼", "").Trim()).ToList();

    private static string Cell(AngleSharp.Dom.IElement row, int index) =>
        row.QuerySelectorAll("td")[index].TextContent.Trim();
}
