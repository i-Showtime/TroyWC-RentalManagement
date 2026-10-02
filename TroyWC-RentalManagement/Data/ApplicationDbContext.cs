using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TroyWC_RentalManagement.bogus;
using TroyWC_RentalManagement.Models;

namespace TroyWC_RentalManagement.Data;

public class ApplicationDbContext(DbContextOptions options)
: IdentityDbContext<IdentityUser>(options)
{

   public DbSet<Property> Properties {get; set;}
    public DbSet<Unit> Units {get; set;}
    public DbSet<Lease> Leases {get; set;}
    public DbSet<RentalApplication> RentalApplications {get; set;}
    public DbSet<ApplicationApplicant> ApplicationApplicants {get; set;}
    public DbSet<Residence> Residences {get; set;}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
         base.OnModelCreating(modelBuilder);
         //modelBuilder.Entity<Address>().HasData(FakeData.CreateAddresses(10));

        modelBuilder.Entity<RentalApplication>(b =>
        {
            b.Property(a => a.Status).HasConversion<string>().HasMaxLength(50);

            b.HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(a => a.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // A unit can have only one active lease. The check in the approve action gives the friendly error;
        // this catches two managers approving different applications for the same unit at the same time.
        modelBuilder.Entity<Lease>(b =>
        {
            b.HasIndex(l => l.UnitId, "IX_Leases_UnitId_Active")
                .IsUnique()
                .HasFilter($"[Status] = {(int)LeaseStatus.Active}");

            // Keep the plain foreign-key index too; the filtered one only covers active leases.
            b.HasIndex(l => l.UnitId);
        });

        modelBuilder.Entity<ApplicationApplicant>()
            .HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

}

