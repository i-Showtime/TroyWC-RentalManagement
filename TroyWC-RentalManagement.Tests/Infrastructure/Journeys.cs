using System.Text.RegularExpressions;

namespace TroyWC_RentalManagement.Tests.Infrastructure;

public static class Urls
{
    public const string Home = "/?pageSize=100";

    public static string Apply(int unitId) => $"/Applicant/Applications/Apply/{unitId}";

    public static string ApplicantStep(int id) => $"/Applicant/Applications/{id}/Applicant";

    public static string ResidencesStep(int id) => $"/Applicant/Applications/{id}/Residences";

    public static string ReviewStep(int id) => $"/Applicant/Applications/{id}/Review";

    public static string Submit(int id) => $"/Applicant/Applications/{id}/Submit";

    public const string ManageApplications = "/Manage/Applications?pageSize=100";

    public static string ManageApplication(int id) => $"/Manage/Applications/{id}";

    public static string ManageDecision(int id, string action) => $"/Manage/Applications/{id}/{action}";

    /// <summary>The application id in a wizard URL such as /Applicant/Applications/12/Residences.</summary>
    public static int ApplicationId(string url) =>
        int.Parse(Regex.Match(url, @"/Applicant/Applications/(\d+)/").Groups[1].Value);
}

/// <summary>Form values for the wizard.</summary>
public static class Forms
{
    public static Dictionary<string, string?> ApplicantInfo(string command = "Continue") => new()
    {
        ["Command"] = command,
        ["Applicant.FirstName"] = "Ada",
        ["Applicant.LastName"] = "Lovelace",
        ["Applicant.Phone"] = "518-555-0100",
        ["Applicant.Email"] = "ada@example.com",
        ["Applicant.CurrentAddress.Line1"] = "5 Elm St",
        ["Applicant.CurrentAddress.City"] = "Troy",
        ["Applicant.CurrentAddress.State"] = "NY",
        ["Applicant.CurrentAddress.PostalCode"] = "12180",
    };

    /// <summary>A valid prior residence under <paramref name="prefix"/>, e.g. "ResidenceDraft" or "Residences[0]".</summary>
    public static Dictionary<string, string?> Residence(
        string prefix, string landlord = "Pat Landlord", string moveIn = "2020-01-01", string moveOut = "2023-06-30") => new()
    {
        [$"{prefix}.Address.Line1"] = "9 Oak Ave",
        [$"{prefix}.Address.City"] = "Albany",
        [$"{prefix}.Address.State"] = "NY",
        [$"{prefix}.Address.PostalCode"] = "12203",
        [$"{prefix}.LandlordName"] = landlord,
        [$"{prefix}.LandlordPhone"] = "518-555-0199",
        [$"{prefix}.MoveInDate"] = moveIn,
        [$"{prefix}.MoveOutDate"] = moveOut,
    };

    public static Dictionary<string, string?> With(this Dictionary<string, string?> form, params (string Key, string? Value)[] values)
    {
        var copy = new Dictionary<string, string?>(form);
        foreach (var (key, value) in values)
            copy[key] = value;
        return copy;
    }

    public static Dictionary<string, string?> With(this Dictionary<string, string?> form, Dictionary<string, string?> more)
    {
        var copy = new Dictionary<string, string?>(form);
        foreach (var (key, value) in more)
            copy[key] = value;
        return copy;
    }
}

/// <summary>Common paths through the app, built only from real requests.</summary>
public static class Journeys
{
    /// <summary>Applies for <paramref name="unitId"/> and completes step 1; returns the new application's id.</summary>
    public static async Task<int> StartApplicationAsync(this AppClient client, int unitId)
    {
        var page = await client.GetAsync(Urls.Apply(unitId));
        var location = await client.PostForRedirectAsync(Urls.Apply(unitId), page, Forms.ApplicantInfo());
        return Urls.ApplicationId(location);
    }

    /// <summary>Completes step 2 with <paramref name="residences"/> prior residences.</summary>
    public static async Task CompleteResidencesAsync(this AppClient client, int id, int residences = 1)
    {
        var page = await client.GetAsync(Urls.ResidencesStep(id));
        var form = new Dictionary<string, string?> { ["Command"] = "Continue", ["RowVersion"] = page.Value("RowVersion") };
        for (var i = 0; i < residences; i++)
            form = form.With(Forms.Residence($"Residences[{i}]"));

        var location = await client.PostForRedirectAsync(Urls.ResidencesStep(id), page, form);
        Assert.EndsWith($"/{id}/Review", location);
    }

    /// <summary>Presses Submit on the review step and returns the response.</summary>
    public static async Task<HttpResponseMessage> PressSubmitAsync(this AppClient client, int id)
    {
        var review = await client.GetAsync(Urls.ReviewStep(id));
        return await client.PostAsync(Urls.Submit(id), review, new Dictionary<string, string?> { ["RowVersion"] = review.Value("RowVersion") });
    }

    /// <summary>Applies for <paramref name="unitId"/>, completes both sections and submits; returns the application id.</summary>
    public static async Task<int> SubmitApplicationAsync(this AppClient client, int unitId)
    {
        var id = await client.StartApplicationAsync(unitId);
        await client.CompleteResidencesAsync(id);
        var response = await client.PressSubmitAsync(id);
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        return id;
    }

    /// <summary>
    /// Presses a decision button ("StartReview", "Approve", "Return", "Deny") on the manager's application page.
    /// The form is posted as the page renders it (row version, default lease dates) with <paramref name="changes"/>
    /// applied; an empty string posts a blank field.
    /// </summary>
    public static async Task<HttpResponseMessage> DecideAsync(
        this AppClient manager, int id, string action, Dictionary<string, string?>? changes = null)
    {
        var page = await manager.GetAsync(Urls.ManageApplication(id));
        var form = new Dictionary<string, string?>
        {
            ["Decision.RowVersion"] = page.Value("Decision.RowVersion"),
            ["Decision.LeaseStart"] = page.Value("Decision.LeaseStart"),
            ["Decision.LeaseEnd"] = page.Value("Decision.LeaseEnd"),
        };
        return await manager.PostAsync(Urls.ManageDecision(id, action), page, form.With(changes ?? []));
    }

    public static Dictionary<string, string?> Comment(string text) => new() { ["Decision.Comment"] = text };

    public static Dictionary<string, string?> LeaseDates(string start, string end) => new()
    {
        ["Decision.LeaseStart"] = start,
        ["Decision.LeaseEnd"] = end,
    };
}
