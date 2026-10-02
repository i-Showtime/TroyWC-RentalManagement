namespace TroyWC_RentalManagement.Models;

/// <summary>The status changes a rental application can make, and the history each one leaves.</summary>
public static class ApplicationWorkflow
{
    private static readonly Dictionary<AppStatus, AppStatus[]> Allowed = new()
    {
        // Applicant
        [AppStatus.Draft] = [AppStatus.Submitted],
        [AppStatus.Returned] = [AppStatus.Submitted],

        // Property manager
        [AppStatus.Submitted] = [AppStatus.UnderReview, AppStatus.Returned, AppStatus.Denied],
        [AppStatus.UnderReview] = [AppStatus.Returned, AppStatus.Approved, AppStatus.Denied],
    };

    public static bool CanMove(AppStatus from, AppStatus to) =>
        Allowed.TryGetValue(from, out var next) && next.Contains(to);

    /// <summary>
    /// Moves <paramref name="application"/> to <paramref name="to"/>, stamps Submitted/Approved/Updated and records
    /// the change in its History. Check <see cref="CanMove"/> first; an invalid move throws.
    /// </summary>
    public static void Move(
        RentalApplication application, AppStatus to, string actorUserId, string actorRole, DateTimeOffset now)
    {
        if (!CanMove(application.Status, to))
            throw new InvalidOperationException($"A {application.Status} application can't move to {to}.");

        application.History.Add(new ApplicationHistory
        {
            OccurredTime = now,
            ActorUserId = actorUserId,
            ActorRole = actorRole,
            EventType = to.ToString(),
            FromStatus = application.Status.ToString(),
            ToStatus = to.ToString(),
        });

        application.Status = to;
        application.Updated = now;

        if (to == AppStatus.Submitted)
            application.Submitted = now;
        else if (to == AppStatus.Approved)
            application.Approved = now;
    }
}

public static class AppStatusExtensions
{
    public static string ToLabel(this AppStatus status) =>
        status == AppStatus.UnderReview ? "Under review" : status.ToString();

    /// <summary>Bootstrap badge colour for the status.</summary>
    public static string ToBadgeClass(this AppStatus status) => status switch
    {
        AppStatus.Draft or AppStatus.Returned => "text-bg-secondary",
        AppStatus.Approved => "text-bg-success",
        AppStatus.Denied or AppStatus.Withdrawn => "text-bg-danger",
        _ => "text-bg-primary",
    };
}
