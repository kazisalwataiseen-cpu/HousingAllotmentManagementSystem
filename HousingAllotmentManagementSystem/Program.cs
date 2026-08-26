using HousingAllotmentManagementSystem.Data;
using HousingAllotmentManagementSystem.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// SERVICES
// =========================================================

// MVC
builder.Services.AddControllersWithViews();


// =========================================================
// DATABASE
// =========================================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));


// =========================================================
// EMAIL SERVICE
// =========================================================

builder.Services.AddScoped<IEmailService, EmailService>();


// =========================================================
// COOKIE AUTHENTICATION
// =========================================================

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Login page
        options.LoginPath = "/Account/Login";

        // Logout page
        options.LogoutPath = "/Account/Logout";

        // Access denied page
        options.AccessDeniedPath = "/Account/AccessDenied";

        // Login cookie duration
        options.ExpireTimeSpan = TimeSpan.FromHours(8);

        // Keep user logged in while active
        options.SlidingExpiration = true;
    });


// =========================================================
// BUILD APPLICATION
// =========================================================

var app = builder.Build();


// =========================================================
// HTTP REQUEST PIPELINE
// =========================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}


// HTTPS
app.UseHttpsRedirection();


// Static files from wwwroot
app.UseStaticFiles();


// Routing
app.UseRouting();


// IMPORTANT:
// Authentication must come BEFORE Authorization
app.UseAuthentication();

app.UseAuthorization();


// =========================================================
// DEFAULT ROUTE
// =========================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


// =========================================================
// RUN
// =========================================================

app.Run();