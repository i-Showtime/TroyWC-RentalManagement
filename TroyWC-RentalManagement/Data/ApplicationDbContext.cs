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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
         base.OnModelCreating(modelBuilder);         
         //modelBuilder.Entity<Address>().HasData(FakeData.CreateAddresses(10));
    }

}


