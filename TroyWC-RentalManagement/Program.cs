using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using TroyWC_RentalManagement.bogus;
using TroyWC_RentalManagement.Data;
using TroyWC_RentalManagement.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddOpenApi();
builder.Services.AddControllersWithViews();

builder.Services.AddDefaultIdentity<IdentityUser>(options => 
    options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();

builder.Services.Configure<SeedDataOptions>(builder.Configuration.GetSection(SeedDataOptions.SectionName));
builder.Services.AddScoped<DevDataSeeder>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate(); // Apply pending migrations   
}
    app.UseMigrationsEndPoint();
    app.MapOpenApi();
    app.MapScalarApiReference();    
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.MapControllers();

app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    if (!await roleManager.RoleExistsAsync(Roles.Applicant))
       _ = await roleManager.CreateAsync(new IdentityRole(Roles.Applicant));

    if (!await roleManager.RoleExistsAsync(Roles.PropertyManager))
       _ = await roleManager.CreateAsync(new IdentityRole(Roles.PropertyManager));

    // Test data from bogus/DevDataSeeder.cs; logins are in appsettings.Development.json (SeedData).
    if (app.Environment.IsDevelopment())
        await scope.ServiceProvider.GetRequiredService<DevDataSeeder>().SeedAsync();
}

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
