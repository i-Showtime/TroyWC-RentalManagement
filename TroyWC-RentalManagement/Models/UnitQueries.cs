using System.Linq.Expressions;

namespace TroyWC_RentalManagement.Models;

public static class UnitQueries
{
    /// <summary>
    /// A unit can be applied for while it and its property exist and it has no active lease.
    /// Used by the home page, when starting an application and when submitting one.
    /// </summary>
    public static readonly Expression<Func<Unit, bool>> IsAvailable = u =>
        !u.IsDeleted
        && !u.Property.IsDeleted
        && !u.Leases.Any(l => l.Status == LeaseStatus.Active);
}
