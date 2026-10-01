namespace TroyWC_RentalManagement.Grid;

public static class HttpRequestExtensions
{
    /// <summary>True for requests sent by wwwroot/js/crud-grid.js, which expect a partial view instead of a full page.</summary>
    public static bool IsAjax(this HttpRequest request) =>
        request.Headers.XRequestedWith == "XMLHttpRequest";
}
