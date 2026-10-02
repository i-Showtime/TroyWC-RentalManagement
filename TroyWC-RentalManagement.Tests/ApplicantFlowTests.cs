using System.Net;
using Microsoft.EntityFrameworkCore;
using TroyWC_RentalManagement.Models;
using TroyWC_RentalManagement.Tests.Infrastructure;

namespace TroyWC_RentalManagement.Tests;

/// <summary>The applicant's side end to end: finding a unit, the three-step wizard, and submitting.</summary>
[Collection(AppCollection.Name)]
public sealed class ApplicantFlowTests(RentalAppFactory factory)
{
    private readonly TestData _data = new(factory);

    [Fact]
    public async Task Home_lists_available_units_and_only_applicants_get_an_apply_button()
    {
        var available = await _data.CreateUnitAsync();
        var leased = await _data.CreateUnitAsync();
        await _data.CreateLeaseAsync(leased);
        var (_, applicant) = await _data.SignInNewAsync(Roles.Applicant);
        var (_, manager) = await _data.SignInNewAsync(Roles.PropertyManager);

        var anonymousHome = await factory.CreateAppClient().GetAsync(Urls.Home);
        var applicantHome = await applicant.GetAsync(Urls.Home);
        var managerHome = await manager.GetAsync(Urls.Home);

        Assert.NotNull(anonymousHome.Find($"tr[data-key=\"{available}\"]"));
        Assert.Null(anonymousHome.Find($"tr[data-key=\"{leased}\"]"));
        Assert.NotNull(applicantHome.Find($"a[href=\"{Urls.Apply(available)}\"]"));
        Assert.Null(anonymousHome.Find($"a[href=\"{Urls.Apply(available)}\"]"));
        Assert.Null(managerHome.Find($"a[href=\"{Urls.Apply(available)}\"]"));
    }

    [Fact]
    public async Task Only_signed_in_applicants_can_open_the_wizard()
    {
        var unit = await _data.CreateUnitAsync();
        var (_, manager) = await _data.SignInNewAsync(Roles.PropertyManager);

        var anonymous = await factory.CreateAppClient().GetAsync(Urls.Apply(unit));
        var asManager = await manager.GetAsync(Urls.Apply(unit));

        Assert.Equal(HttpStatusCode.Redirect, anonymous.Status);
        Assert.Contains("/Identity/Account/Login", anonymous.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.Redirect, asManager.Status);
        Assert.Contains("/Identity/Account/AccessDenied", asManager.Location!.OriginalString);
    }

    [Fact]
    public async Task Nothing_is_validated_or_saved_until_a_button_is_pressed()
    {
        var unit = await _data.CreateUnitAsync();
        var (user, applicant) = await _data.SignInNewAsync(Roles.Applicant);

        var page = await applicant.GetAsync(Urls.Apply(unit));

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Empty(page.Errors);
        Assert.Equal(user.Email, page.Value("Applicant.Email"));
        Assert.False(await _data.QueryAsync(db => db.RentalApplications.AnyAsync(a => a.UnitId == unit)));
    }

    [Fact]
    public async Task Continue_with_missing_applicant_info_shows_errors_and_saves_nothing()
    {
        var unit = await _data.CreateUnitAsync();
        var (_, applicant) = await _data.SignInNewAsync(Roles.Applicant);
        var page = await applicant.GetAsync(Urls.Apply(unit));

        var result = await applicant.PostForPageAsync(Urls.Apply(unit), page,
            Forms.ApplicantInfo().With(("Applicant.FirstName", ""), ("Applicant.Email", "not-an-email")));

        Assert.Equal("The First name field is required.", result.ErrorFor("Applicant.FirstName"));
        Assert.Equal("The Email field is not a valid e-mail address.", result.ErrorFor("Applicant.Email"));
        Assert.Equal("Lovelace", result.Value("Applicant.LastName"));
        Assert.False(await _data.QueryAsync(db => db.RentalApplications.AnyAsync(a => a.UnitId == unit)));
    }

    [Fact]
    public async Task Save_keeps_invalid_input_without_showing_errors_and_leaves_the_section_incomplete()
    {
        var unit = await _data.CreateUnitAsync();
        var (_, applicant) = await _data.SignInNewAsync(Roles.Applicant);
        var page = await applicant.GetAsync(Urls.Apply(unit));

        var location = await applicant.PostForRedirectAsync(Urls.Apply(unit), page, new Dictionary<string, string?>
        {
            ["Command"] = "Save",
            ["Applicant.FirstName"] = "Ada",
            ["Applicant.Email"] = "not-an-email",
        });
        var id = Urls.ApplicationId(location);
        var saved = await applicant.GetAsync(location);

        Assert.Equal("Ada", saved.Value("Applicant.FirstName"));
        Assert.Equal("not-an-email", saved.Value("Applicant.Email"));
        Assert.Empty(saved.Errors);
        Assert.True(saved.HasText("Your application has been saved."));
        var application = await _data.LoadApplicationAsync(id);
        Assert.Equal(AppStatus.Draft, application.Status);
        Assert.False(application.ApplicantInfoCompleted);
    }

    [Fact]
    public async Task Applicant_completes_every_step_and_submits()
    {
        var unit = await _data.CreateUnitAsync();
        var (user, applicant) = await _data.SignInNewAsync(Roles.Applicant);

        // Step 1: applicant info.
        var step1 = await applicant.GetAsync(Urls.Apply(unit));
        var location = await applicant.PostForRedirectAsync(Urls.Apply(unit), step1, Forms.ApplicantInfo());
        var id = Urls.ApplicationId(location);
        Assert.EndsWith($"/{id}/Residences", location);

        // Step 2: an invalid residence reopens the modal with its errors and is not kept.
        var step2 = await applicant.GetAsync(Urls.ResidencesStep(id));
        Assert.True(step2.HasText("No prior residences."));
        var invalid = await applicant.PostForPageAsync(Urls.ResidencesStep(id), step2,
            Forms.Residence("ResidenceDraft", moveIn: "2023-01-01", moveOut: "2022-01-01").With(("Command", "SaveResidence")));
        Assert.Equal("true", invalid.Find("#residence-modal")!.GetAttribute("data-show-modal"));
        Assert.Equal("Move-out date can't be before the move-in date.", invalid.ErrorFor("ResidenceDraft.MoveOutDate"));
        Assert.Null(invalid.Value("Residences[0].LandlordName"));

        // A valid residence joins the list in the form, but nothing is saved yet...
        var added = await applicant.PostForPageAsync(Urls.ResidencesStep(id), step2,
            Forms.Residence("ResidenceDraft").With(("Command", "SaveResidence")));
        Assert.Equal("Pat Landlord", added.Value("Residences[0].LandlordName"));
        Assert.Equal("false", added.Find("#residence-modal")!.GetAttribute("data-show-modal"));
        Assert.Empty((await _data.LoadApplicationAsync(id)).Applicants.Single().Residences);

        // ...so Back (a plain link away) and returning discards it.
        Assert.True((await applicant.GetAsync(Urls.ResidencesStep(id))).HasText("No prior residences."));

        // Continue saves the residences that are posted.
        await applicant.CompleteResidencesAsync(id, residences: 2);

        // Step 3: read-only review, then submit.
        var review = await applicant.GetAsync(Urls.ReviewStep(id));
        Assert.True(review.HasText("Ada Lovelace"));
        Assert.True(review.HasText("9 Oak Ave"));
        Assert.Empty(review.FindAll("input:not([type=hidden]), textarea, select"));

        var submitted = await applicant.PressSubmitAsync(id);
        Assert.Equal(HttpStatusCode.Redirect, submitted.StatusCode);
        Assert.True((await applicant.GetAsync(Urls.ReviewStep(id))).HasText("Your application has been submitted."));

        var application = await _data.LoadApplicationAsync(id);
        Assert.Equal(AppStatus.Submitted, application.Status);
        Assert.NotNull(application.Submitted);
        Assert.Equal(user.Id, application.CreatedByUserId);
        var applicantRow = application.Applicants.Single();
        Assert.True(applicantRow.IsPrimary);
        Assert.Equal(unit, applicantRow.UnitId);
        Assert.Equal("Ada", applicantRow.FirstName);
        Assert.Equal(2, applicantRow.Residences.Count);
        var history = Assert.Single(application.History);
        Assert.Equal(("Draft", "Submitted", Roles.Applicant), (history.FromStatus, history.ToStatus, history.ActorRole));
    }

    [Fact]
    public async Task Submitted_applications_are_read_only()
    {
        var unit = await _data.CreateUnitAsync();
        var (_, applicant) = await _data.SignInNewAsync(Roles.Applicant);
        var id = await applicant.SubmitApplicationAsync(unit);

        var step1 = await applicant.GetAsync(Urls.ApplicantStep(id));
        Assert.NotNull(step1.Find("fieldset[disabled]"));
        Assert.Null(step1.Find("button[value=Save]"));

        var location = await applicant.PostForRedirectAsync(Urls.ApplicantStep(id), step1,
            Forms.ApplicantInfo("Save").With(("Applicant.FirstName", "Changed")));
        Assert.True((await applicant.GetAsync(location)).HasText("This application can no longer be changed."));
        Assert.Equal("Ada", (await _data.LoadApplicationAsync(id)).Applicants.Single().FirstName);
    }

    [Fact]
    public async Task Residences_can_be_edited_and_removed_before_they_are_saved()
    {
        var unit = await _data.CreateUnitAsync();
        var (_, applicant) = await _data.SignInNewAsync(Roles.Applicant);
        var id = await applicant.StartApplicationAsync(unit);
        var page = await applicant.GetAsync(Urls.ResidencesStep(id));
        var two = Forms.Residence("Residences[0]", landlord: "First").With(Forms.Residence("Residences[1]", landlord: "Second"));

        var edited = await applicant.PostForPageAsync(Urls.ResidencesStep(id), page, two
            .With(Forms.Residence("ResidenceDraft", landlord: "Edited"))
            .With(("Command", "SaveResidence"), ("ResidenceDraftIndex", "1")));
        Assert.Equal("First", edited.Value("Residences[0].LandlordName"));
        Assert.Equal("Edited", edited.Value("Residences[1].LandlordName"));

        var removed = await applicant.PostForPageAsync($"{Urls.ResidencesStep(id)}?commandIndex=0", page, two
            .With(("Command", "RemoveResidence")));
        Assert.Equal("Second", removed.Value("Residences[0].LandlordName"));
        Assert.Null(removed.Value("Residences[1].LandlordName"));
        Assert.Empty((await _data.LoadApplicationAsync(id)).Applicants.Single().Residences);
    }

    [Fact]
    public async Task Continue_with_an_invalid_residence_points_at_it_and_saves_nothing()
    {
        var unit = await _data.CreateUnitAsync();
        var (_, applicant) = await _data.SignInNewAsync(Roles.Applicant);
        var id = await applicant.StartApplicationAsync(unit);
        var page = await applicant.GetAsync(Urls.ResidencesStep(id));

        var result = await applicant.PostForPageAsync(Urls.ResidencesStep(id), page, Forms.Residence("Residences[0]")
            .With(("Residences[0].LandlordPhone", ""), ("Command", "Continue"), ("RowVersion", page.Value("RowVersion"))));

        Assert.True(result.HasText("Edit this residence to fix: The Landlord phone field is required."));
        var application = await _data.LoadApplicationAsync(id);
        Assert.Empty(application.Applicants.Single().Residences);
        Assert.False(application.ResidenceHistoryCompleted);
    }

    [Fact]
    public async Task An_applicant_with_no_prior_residences_can_submit()
    {
        var unit = await _data.CreateUnitAsync();
        var (_, applicant) = await _data.SignInNewAsync(Roles.Applicant);
        var id = await applicant.StartApplicationAsync(unit);

        await applicant.CompleteResidencesAsync(id, residences: 0);
        var response = await applicant.PressSubmitAsync(id);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(AppStatus.Submitted, (await _data.LoadApplicationAsync(id)).Status);
    }

    [Fact]
    public async Task Submit_is_refused_until_both_sections_are_complete()
    {
        var unit = await _data.CreateUnitAsync();
        var (_, applicant) = await _data.SignInNewAsync(Roles.Applicant);
        var id = await applicant.StartApplicationAsync(unit);

        var review = await applicant.GetAsync(Urls.ReviewStep(id));
        Assert.NotNull(review.Find("button.btn-success[disabled]"));

        var result = await HtmlPage.ReadAsync(await applicant.PressSubmitAsync(id));

        Assert.Equal(HttpStatusCode.OK, result.Status);
        Assert.Contains("Complete the residence history section before submitting.", result.Errors);
        Assert.Equal(AppStatus.Draft, (await _data.LoadApplicationAsync(id)).Status);
    }

    [Fact]
    public async Task Submit_is_refused_once_the_unit_has_been_leased()
    {
        var unit = await _data.CreateUnitAsync();
        var (_, applicant) = await _data.SignInNewAsync(Roles.Applicant);
        var id = await applicant.StartApplicationAsync(unit);
        await applicant.CompleteResidencesAsync(id);
        await _data.CreateLeaseAsync(unit);

        var result = await HtmlPage.ReadAsync(await applicant.PressSubmitAsync(id));

        Assert.Contains("This unit has been leased and is no longer accepting applications.", result.Errors);
        Assert.Equal(AppStatus.Draft, (await _data.LoadApplicationAsync(id)).Status);
    }

    [Fact]
    public async Task Several_applicants_can_apply_for_a_unit_and_applying_again_resumes_your_application()
    {
        var unit = await _data.CreateUnitAsync();
        var (_, first) = await _data.SignInNewAsync(Roles.Applicant);
        var (_, second) = await _data.SignInNewAsync(Roles.Applicant);

        var firstId = await first.SubmitApplicationAsync(unit);
        var secondId = await second.SubmitApplicationAsync(unit);
        var again = await first.GetAsync(Urls.Apply(unit));

        Assert.NotEqual(firstId, secondId);
        Assert.Equal(HttpStatusCode.Redirect, again.Status);
        Assert.EndsWith(Urls.ApplicantStep(firstId), again.Location!.OriginalString);
    }

    [Fact]
    public async Task Applicants_cannot_open_each_others_applications()
    {
        var unit = await _data.CreateUnitAsync();
        var (_, owner) = await _data.SignInNewAsync(Roles.Applicant);
        var (_, other) = await _data.SignInNewAsync(Roles.Applicant);
        var id = await owner.StartApplicationAsync(unit);
        var otherPage = await other.GetAsync(Urls.Apply(unit));

        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(Urls.ReviewStep(id))).Status);
        var post = await other.PostAsync(Urls.ApplicantStep(id), otherPage, Forms.ApplicantInfo("Save"));
        Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
    }

    [Fact]
    public async Task Saving_over_a_newer_version_is_refused()
    {
        var unit = await _data.CreateUnitAsync();
        var (_, applicant) = await _data.SignInNewAsync(Roles.Applicant);
        var id = await applicant.StartApplicationAsync(unit);
        var stale = await applicant.GetAsync(Urls.ApplicantStep(id));
        var staleForm = Forms.ApplicantInfo("Save").With(("RowVersion", stale.Value("RowVersion")));

        await applicant.PostForRedirectAsync(Urls.ApplicantStep(id), stale, staleForm.With(("Applicant.FirstName", "First")));
        var location = await applicant.PostForRedirectAsync(Urls.ApplicantStep(id), stale, staleForm.With(("Applicant.FirstName", "Second")));

        Assert.True((await applicant.GetAsync(location)).HasText("changed in another window"));
        Assert.Equal("First", (await _data.LoadApplicationAsync(id)).Applicants.Single().FirstName);
    }
}
