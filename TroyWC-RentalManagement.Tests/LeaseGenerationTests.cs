using System.Net;
using Microsoft.EntityFrameworkCore;
using TroyWC_RentalManagement.Models;
using TroyWC_RentalManagement.Tests.Infrastructure;

namespace TroyWC_RentalManagement.Tests;

/// <summary>Property managers reviewing submitted applications, and the lease created when one is approved.</summary>
[Collection(AppCollection.Name)]
public sealed class LeaseGenerationTests(RentalAppFactory factory)
{
    private readonly TestData _data = new(factory);

    [Fact]
    public async Task Approving_an_application_creates_an_active_lease_for_its_unit()
    {
        var unit = await _data.CreateUnitAsync();
        var (_, applicant) = await _data.SignInNewAsync(Roles.Applicant);
        var (manager, managerClient) = await _data.SignInNewAsync(Roles.PropertyManager);
        var id = await applicant.SubmitApplicationAsync(unit);

        // The manager finds it in the list and opens it.
        var list = await managerClient.GetAsync(Urls.ManageApplications);
        Assert.NotNull(list.Find($"tr[data-key=\"{id}\"] a[href=\"{Urls.ManageApplication(id)}\"]"));

        Assert.Equal(HttpStatusCode.Redirect, (await managerClient.DecideAsync(id, "StartReview")).StatusCode);
        var approve = await managerClient.DecideAsync(id, "Approve",
            Journeys.LeaseDates("2027-01-01", "2027-12-31").With(Journeys.Comment("Welcome!")));
        Assert.Equal(HttpStatusCode.Redirect, approve.StatusCode);

        var lease = Assert.Single(await _data.LeasesForUnitAsync(unit));
        Assert.Equal(id, lease.ApplicationId);
        Assert.Equal(LeaseStatus.Active, lease.Status);
        Assert.Equal(new DateOnly(2027, 1, 1), lease.StartDate);
        Assert.Equal(new DateOnly(2027, 12, 31), lease.EndDate);

        var application = await _data.LoadApplicationAsync(id);
        Assert.Equal(AppStatus.Approved, application.Status);
        Assert.NotNull(application.Approved);
        Assert.Equal(manager.Id, application.AssignedManagerId);
        Assert.Equal(new[] { "Submitted", "UnderReview", "Approved" },
            application.History.OrderBy(h => h.OccurredTime).Select(h => h.ToStatus!).ToArray());
        Assert.Equal("Welcome!", Assert.Single(application.Comments).Body);

        // The manager's page shows the lease; the unit leaves the home page; the applicant sees a read-only approval.
        var details = await managerClient.GetAsync(Urls.ManageApplication(id));
        Assert.True(details.HasText("The application was approved and its lease was created."));
        Assert.True(details.HasText("Jan 1, 2027 – Dec 31, 2027"));
        Assert.Null(details.Find("button[formaction$=\"/Approve\"]"));
        Assert.Null((await applicant.GetAsync(Urls.Home)).Find($"tr[data-key=\"{unit}\"]"));
        var applicantView = await applicant.GetAsync(Urls.ReviewStep(id));
        Assert.True(applicantView.HasText("This application is approved and can no longer be changed."));
    }

    [Fact]
    public async Task The_approve_form_suggests_a_one_year_lease_from_the_first_of_next_month()
    {
        var unit = await _data.CreateUnitAsync();
        var (_, applicant) = await _data.SignInNewAsync(Roles.Applicant);
        var (_, manager) = await _data.SignInNewAsync(Roles.PropertyManager);
        var id = await applicant.SubmitApplicationAsync(unit);
        await manager.DecideAsync(id, "StartReview");

        var page = await manager.GetAsync(Urls.ManageApplication(id));

        var today = DateOnly.FromDateTime(DateTime.Today);
        var start = new DateOnly(today.Year, today.Month, 1).AddMonths(1);
        Assert.Equal(start.ToString("yyyy-MM-dd"), page.Value("Decision.LeaseStart"));
        Assert.Equal(start.AddYears(1).AddDays(-1).ToString("yyyy-MM-dd"), page.Value("Decision.LeaseEnd"));
    }

    [Fact]
    public async Task Other_applications_for_the_unit_are_left_alone_but_can_no_longer_be_submitted()
    {
        var unit = await _data.CreateUnitAsync();
        var (_, winner) = await _data.SignInNewAsync(Roles.Applicant);
        var (_, waiting) = await _data.SignInNewAsync(Roles.Applicant);
        var (_, drafting) = await _data.SignInNewAsync(Roles.Applicant);
        var (_, manager) = await _data.SignInNewAsync(Roles.PropertyManager);
        var winnerId = await winner.SubmitApplicationAsync(unit);
        var waitingId = await waiting.SubmitApplicationAsync(unit);
        var draftId = await drafting.StartApplicationAsync(unit);
        await drafting.CompleteResidencesAsync(draftId);

        await manager.DecideAsync(winnerId, "StartReview");
        await manager.DecideAsync(winnerId, "Approve");

        Assert.Equal(AppStatus.Submitted, (await _data.LoadApplicationAsync(waitingId)).Status);
        var refused = await HtmlPage.ReadAsync(await drafting.PressSubmitAsync(draftId));
        Assert.Contains("This unit has been leased and is no longer accepting applications.", refused.Errors);
        Assert.Equal(AppStatus.Draft, (await _data.LoadApplicationAsync(draftId)).Status);
    }

    [Fact]
    public async Task Only_applications_under_review_can_be_approved()
    {
        var unit = await _data.CreateUnitAsync();
        var (_, applicant) = await _data.SignInNewAsync(Roles.Applicant);
        var (_, manager) = await _data.SignInNewAsync(Roles.PropertyManager);
        var id = await applicant.SubmitApplicationAsync(unit);

        var page = await manager.GetAsync(Urls.ManageApplication(id));
        Assert.Null(page.Find("button[formaction$=\"/Approve\"]"));

        var response = await manager.DecideAsync(id, "Approve");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True((await manager.GetAsync(Urls.ManageApplication(id))).HasText("so it can't be moved to approved"));
        Assert.Equal(AppStatus.Submitted, (await _data.LoadApplicationAsync(id)).Status);
        Assert.Empty(await _data.LeasesForUnitAsync(unit));
    }

    [Theory]
    [InlineData("", "2027-12-31", "Decision.LeaseStart", "Enter the lease start date.")]
    [InlineData("2027-01-01", "", "Decision.LeaseEnd", "Enter the lease end date.")]
    [InlineData("2027-06-01", "2027-05-31", "Decision.LeaseEnd", "The lease must end after it starts.")]
    public async Task Approval_needs_valid_lease_dates(string start, string end, string field, string message)
    {
        var unit = await _data.CreateUnitAsync();
        var (_, applicant) = await _data.SignInNewAsync(Roles.Applicant);
        var (_, manager) = await _data.SignInNewAsync(Roles.PropertyManager);
        var id = await applicant.SubmitApplicationAsync(unit);
        await manager.DecideAsync(id, "StartReview");

        var result = await HtmlPage.ReadAsync(await manager.DecideAsync(id, "Approve", Journeys.LeaseDates(start, end)));

        Assert.Equal(HttpStatusCode.OK, result.Status);
        Assert.Equal(message, result.ErrorFor(field));
        Assert.Equal(AppStatus.UnderReview, (await _data.LoadApplicationAsync(id)).Status);
        Assert.Empty(await _data.LeasesForUnitAsync(unit));
    }

    [Fact]
    public async Task A_unit_with_an_active_lease_cannot_be_leased_again()
    {
        var unit = await _data.CreateUnitAsync();
        var (_, first) = await _data.SignInNewAsync(Roles.Applicant);
        var (_, second) = await _data.SignInNewAsync(Roles.Applicant);
        var (_, manager) = await _data.SignInNewAsync(Roles.PropertyManager);
        var firstId = await first.SubmitApplicationAsync(unit);
        var secondId = await second.SubmitApplicationAsync(unit);
        await manager.DecideAsync(firstId, "StartReview");
        await manager.DecideAsync(secondId, "StartReview");

        await manager.DecideAsync(firstId, "Approve");
        var result = await HtmlPage.ReadAsync(await manager.DecideAsync(secondId, "Approve"));

        Assert.Contains("This unit already has an active lease.", result.Errors);
        Assert.Equal(AppStatus.UnderReview, (await _data.LoadApplicationAsync(secondId)).Status);
        Assert.Equal(firstId, Assert.Single(await _data.LeasesForUnitAsync(unit)).ApplicationId);
    }

    [Fact]
    public async Task A_unit_whose_lease_has_ended_can_be_leased_again()
    {
        var unit = await _data.CreateUnitAsync();
        await _data.CreateLeaseAsync(unit, LeaseStatus.Inactive);
        var (_, applicant) = await _data.SignInNewAsync(Roles.Applicant);
        var (_, manager) = await _data.SignInNewAsync(Roles.PropertyManager);
        var id = await applicant.SubmitApplicationAsync(unit);
        await manager.DecideAsync(id, "StartReview");

        var response = await manager.DecideAsync(id, "Approve");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var leases = await _data.LeasesForUnitAsync(unit);
        Assert.Equal(2, leases.Count);
        Assert.Equal(id, Assert.Single(leases, l => l.Status == LeaseStatus.Active).ApplicationId);
    }

    [Fact]
    public async Task The_database_allows_only_one_active_lease_per_unit()
    {
        var unit = await _data.CreateUnitAsync();
        await _data.CreateLeaseAsync(unit, LeaseStatus.Active);
        await _data.CreateLeaseAsync(unit, LeaseStatus.Inactive);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => _data.CreateLeaseAsync(unit, LeaseStatus.Active));

        Assert.Contains("IX_Leases_UnitId_Active", ex.InnerException!.Message);
    }

    [Fact]
    public async Task Returning_needs_a_comment_and_lets_the_applicant_fix_and_resubmit()
    {
        var unit = await _data.CreateUnitAsync();
        var (_, applicant) = await _data.SignInNewAsync(Roles.Applicant);
        var (_, manager) = await _data.SignInNewAsync(Roles.PropertyManager);
        var id = await applicant.SubmitApplicationAsync(unit);

        var noComment = await HtmlPage.ReadAsync(await manager.DecideAsync(id, "Return"));
        Assert.Equal("Tell the applicant what to change before returning the application.", noComment.ErrorFor("Decision.Comment"));
        Assert.Equal(AppStatus.Submitted, (await _data.LoadApplicationAsync(id)).Status);

        var returned = await manager.DecideAsync(id, "Return", Journeys.Comment("Please add your landlord's phone number."));
        Assert.Equal(HttpStatusCode.Redirect, returned.StatusCode);

        // The applicant sees why, can edit again, and resubmits.
        var step1 = await applicant.GetAsync(Urls.ApplicantStep(id));
        Assert.True(step1.HasText("Message from the property manager: Please add your landlord's phone number."));
        var location = await applicant.PostForRedirectAsync(Urls.ApplicantStep(id), step1,
            Forms.ApplicantInfo().With(("Applicant.Phone", "518-555-0123"), ("RowVersion", step1.Value("RowVersion"))));
        Assert.EndsWith("/Residences", location);
        Assert.Equal(HttpStatusCode.Redirect, (await applicant.PressSubmitAsync(id)).StatusCode);

        var application = await _data.LoadApplicationAsync(id);
        Assert.Equal(AppStatus.Submitted, application.Status);
        Assert.Equal("518-555-0123", application.Applicants.Single().Phone);
        Assert.Equal(new[] { "Submitted", "Returned", "Submitted" },
            application.History.OrderBy(h => h.OccurredTime).Select(h => h.ToStatus!).ToArray());
    }

    [Fact]
    public async Task Denied_applications_get_no_lease_and_are_read_only()
    {
        var unit = await _data.CreateUnitAsync();
        var (_, applicant) = await _data.SignInNewAsync(Roles.Applicant);
        var (_, manager) = await _data.SignInNewAsync(Roles.PropertyManager);
        var id = await applicant.SubmitApplicationAsync(unit);
        await manager.DecideAsync(id, "StartReview");

        await manager.DecideAsync(id, "Deny", Journeys.Comment("We could not verify your rental history."));

        Assert.Equal(AppStatus.Denied, (await _data.LoadApplicationAsync(id)).Status);
        Assert.Empty(await _data.LeasesForUnitAsync(unit));
        Assert.Null((await manager.GetAsync(Urls.ManageApplication(id))).Find("button[formaction]"));
        var applicantView = await applicant.GetAsync(Urls.ApplicantStep(id));
        Assert.NotNull(applicantView.Find("fieldset[disabled]"));
        Assert.True(applicantView.HasText("We could not verify your rental history."));
    }

    [Fact]
    public async Task Managers_can_see_drafts_but_not_decide_on_them()
    {
        var unit = await _data.CreateUnitAsync();
        var (_, applicant) = await _data.SignInNewAsync(Roles.Applicant);
        var (_, manager) = await _data.SignInNewAsync(Roles.PropertyManager);
        var id = await applicant.StartApplicationAsync(unit);

        var list = await manager.GetAsync($"{Urls.ManageApplications}&status=Draft");
        Assert.NotNull(list.Find($"tr[data-key=\"{id}\"]"));

        var details = await manager.GetAsync(Urls.ManageApplication(id));
        Assert.Equal(HttpStatusCode.OK, details.Status);
        Assert.True(details.HasText("The applicant hasn't submitted this application yet"));
        Assert.True(details.HasText("Ada Lovelace"));
        Assert.Null(details.Find("button[formaction]"));

        // Posting a decision anyway is refused.
        var response = await manager.DecideAsync(id, "StartReview");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True((await manager.GetAsync(Urls.ManageApplication(id))).HasText("This application is draft, so it can't be moved to under review."));
        Assert.Equal(AppStatus.Draft, (await _data.LoadApplicationAsync(id)).Status);
    }

    [Fact]
    public async Task Applicants_cannot_review_applications()
    {
        var unit = await _data.CreateUnitAsync();
        var (_, applicant) = await _data.SignInNewAsync(Roles.Applicant);
        var id = await applicant.SubmitApplicationAsync(unit);

        var page = await applicant.GetAsync(Urls.ManageApplication(id));

        Assert.Equal(HttpStatusCode.Redirect, page.Status);
        Assert.Contains("/Identity/Account/AccessDenied", page.Location!.OriginalString);
    }
}
