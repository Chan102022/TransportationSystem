using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TransportationBookingSystem.Models;
using TransportationBookingSystem.Services;
using TransportationBookingSystem.Data;   // <-- Add this

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Add DB context
builder.Services.AddDbContext<TransportationBookingSystemDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<TransportationBookingSystemDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();

// Add custom services
builder.Services.AddScoped<PassengersService>();

var app = builder.Build();

// ----------------------
// 🔥 Run Seeder Here
// ----------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedData.Initialize(services);   // <-- Runs role + conductor + admin seeding
}
// ----------------------

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
