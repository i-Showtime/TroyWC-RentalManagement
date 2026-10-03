using System.Net;
using TroyWC_RentalManagement.Models;
using TroyWC_RentalManagement.Tests.Infrastructure;

namespace TroyWC_RentalManagement.Tests;

/// <summary>Every grid's page links must lead somewhere (they once rendered href="" for page 2 and beyond).</summary>
[Collection(AppCollection.Name)]
public sealed class GridPagingTests(RentalAppFactory factory)
{
    private readonly TestData _data = new(factory);

    [Theory]
    [InlineData("/", null)]
    [InlineData("/", Roles.Applicant)]
    [InlineData("/Manage/Units", Roles.PropertyManager)]
    [InlineData("/Manage/Properties", Roles.PropertyManager)]
    [InlineData("/Manage/Applications", Roles.PropertyManager)]
    public async Task Next_page_link_shows_the_second_page(string path, string? role)
    {
        // Two of everything, so a one-row page always has a second page.
        var (_, applicant) = await _data.SignInNewAsync(Roles.Applicant);
        await applicant.StartApplicationAsync(await _data.CreateUnitAsync());
        await applicant.StartApplicationAsync(await _data.CreateUnitAsync());
        var client = role is null ? factory.CreateAppClient() : (await _data.SignInNewAsync(role)).Client;

        var first = await client.GetAsync($"{path}?pageSize=1");
        var next = first.FindAll("a.page-link").Single(a => a.TextContent.Trim() == "Next").GetAttribute("href");

        Assert.False(string.IsNullOrEmpty(next), "The Next link has no href.");
        Assert.Contains("pageNumber=2", next);

        var second = await client.GetAsync(next!);
        Assert.Equal(HttpStatusCode.OK, second.Status);
        Assert.StartsWith("Showing 2–2 of", second.Find("small.text-muted")!.TextContent.Trim());
        Assert.NotEqual(RowKey(first), RowKey(second));
        Assert.Equal("2", second.Find(".page-item.active .page-link")!.TextContent.Trim());
    }

    [Fact]
    public async Task Next_page_link_works_for_in_place_grid_updates()
    {
        await _data.CreateUnitAsync();
        await _data.CreateUnitAsync();
        var client = factory.CreateAppClient();

        var first = await client.GetAsync("/?pageSize=1");
        var next = first.FindAll("a.page-link").Single(a => a.TextContent.Trim() == "Next").GetAttribute("href")!;
        var partial = await client.GetAjaxAsync(next);

        Assert.Equal(HttpStatusCode.OK, partial.Status);
        Assert.Null(partial.Find("nav.navbar"));
        Assert.StartsWith("Showing 2–2 of", partial.Find("small.text-muted")!.TextContent.Trim());
    }

    private static string? RowKey(HtmlPage page) => page.Find("tbody tr[data-key]")?.GetAttribute("data-key");
}
