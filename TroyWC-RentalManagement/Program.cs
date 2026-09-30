using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using TroyWC_RentalManagement.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddOpenApi();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.MapOpenApi();
    app.MapScalarApiReference();
    //app.MapScalarApiReference(options =>
    //{
    //    options.WithTitle("Rent API")
    //           .WithClassicLayout()
    //           .ForceDarkMode()
    //           .HideSearch()
    //           .ShowOperationId()
    //           .ExpandAllTags()
    //           .SortTagsAlphabetically()
    //           .SortOperationsByMethod()
    //           .PreserveSchemaPropertyOrder();

    //});
    //app.MapSwagger();
    //app.UseSwaggerUI(options =>
    //{
    //    options.SwaggerEndpoint("v1/swagger.json", "My API V1");
    //});
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
