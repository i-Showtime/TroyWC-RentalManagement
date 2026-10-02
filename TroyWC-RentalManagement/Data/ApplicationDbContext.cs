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

        modelBuilder.Entity<ApplicationApplicant>()
            .HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

}

