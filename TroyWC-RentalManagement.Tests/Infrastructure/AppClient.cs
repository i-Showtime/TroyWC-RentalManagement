using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace TroyWC_RentalManagement.Tests.Infrastructure;

/// <summary>Drives the app like a browser: GETs pages and posts forms with the page's antiforgery token.</summary>
public sealed class AppClient(HttpClient http)
{
    public async Task<HtmlPage> GetAsync(string url) => await HtmlPage.ReadAsync(await http.GetAsync(url));

    /// <summary>GETs <paramref name="url"/> the way wwwroot/js/crud-grid.js does, which returns just the grid partial.</summary>
    public async Task<HtmlPage> GetAjaxAsync(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        return await HtmlPage.ReadAsync(await http.SendAsync(request));
    }

    /// <summary>Posts <paramref name="fields"/> (null values are left out) with <paramref name="from"/>'s antiforgery token.</summary>
    public Task<HttpResponseMessage> PostAsync(string url, HtmlPage from, IEnumerable<KeyValuePair<string, string?>> fields)
    {
        var form = fields
            .Where(f => f.Value is not null)
            .Select(f => new KeyValuePair<string, string>(f.Key, f.Value!))
            .Append(new("__RequestVerificationToken", from.AntiforgeryToken));

        return http.PostAsync(url, new FormUrlEncodedContent(form));
    }

    /// <summary>Posts and reads the page that comes back, which a test expects to be a re-rendered form (no redirect).</summary>
    public async Task<HtmlPage> PostForPageAsync(string url, HtmlPage from, IEnumerable<KeyValuePair<string, string?>> fields)
    {
        var page = await HtmlPage.ReadAsync(await PostAsync(url, from, fields));
        Assert.Equal(HttpStatusCode.OK, page.Status);
        return page;
    }

    /// <summary>Posts, checks the response redirects, and returns where to.</summary>
    public async Task<string> PostForRedirectAsync(string url, HtmlPage from, IEnumerable<KeyValuePair<string, string?>> fields)
    {
        var response = await PostAsync(url, from, fields);
        Assert.True(response.StatusCode == HttpStatusCode.Redirect,
            $"Expected a redirect from {url} but got {(int)response.StatusCode}: {await ErrorSummaryAsync(response)}");
        return response.Headers.Location!.OriginalString;
    }

    private static async Task<string> ErrorSummaryAsync(HttpResponseMessage response)
    {
        var page = await HtmlPage.ReadAsync(response);
        return string.Join(" | ", page.Errors);
    }
}

/// <summary>A response parsed as HTML.</summary>
public sealed partial class HtmlPage
{
    private HtmlPage(HttpStatusCode status, string html, IDocument document, Uri? location)
    {
        Status = status;
        Html = html;
        Document = document;
        Location = location;
    }

    public HttpStatusCode Status { get; }

    public string Html { get; }

    public IDocument Document { get; }

    /// <summary>The redirect target, when the response was a redirect.</summary>
    public Uri? Location { get; }

    public static async Task<HtmlPage> ReadAsync(HttpResponseMessage response)
    {
        var html = await response.Content.ReadAsStringAsync();
        var document = await new HtmlParser().ParseDocumentAsync(html);
        return new HtmlPage(response.StatusCode, html, document, response.Headers.Location);
    }

    public string AntiforgeryToken =>
        Value("__RequestVerificationToken") ?? throw new InvalidOperationException("The page has no antiforgery token.");

    /// <summary>The visible text, whitespace collapsed.</summary>
    public string Text => Whitespace().Replace(Document.Body?.TextContent ?? string.Empty, " ");

    public bool HasText(string text) => Text.Contains(text, StringComparison.Ordinal);

    /// <summary>Value of the input, textarea or select named <paramref name="name"/>; null when there is none.</summary>
    public string? Value(string name)
    {
        var element = Document.QuerySelector($"[name=\"{name}\"]");
        return element switch
        {
            null => null,
            AngleSharp.Html.Dom.IHtmlTextAreaElement textArea => textArea.Value,
            AngleSharp.Html.Dom.IHtmlSelectElement select => select.Value,
            _ => element.GetAttribute("value"),
        };
    }

    public IElement? Find(string selector) => Document.QuerySelector(selector);

    public IReadOnlyList<IElement> FindAll(string selector) => Document.QuerySelectorAll(selector).ToList();

    /// <summary>Validation messages: field errors and the validation summary.</summary>
    public IReadOnlyList<string> Errors => Document
        .QuerySelectorAll(".field-validation-error, .validation-summary-errors li")
        .Select(e => e.TextContent.Trim())
        .Where(t => t.Length > 0)
        .ToList();

    /// <summary>The validation message shown for the field named <paramref name="name"/>.</summary>
    public string? ErrorFor(string name) =>
        Document.QuerySelector($".field-validation-error[data-valmsg-for=\"{name}\"]")?.TextContent.Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
