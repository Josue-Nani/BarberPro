using BarberPro.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using BarberPro.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Habilitar logging en consola para depuración
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddControllersWithViews();
// Add Razor Pages
builder.Services.AddRazorPages();

// Registrar IHttpContextAccessor para inyección en vistas/layout
builder.Services.AddHttpContextAccessor();

// Registrar DbContext con SQL Server
var connectionString = builder.Configuration.GetConnectionString("BarberiaReservas");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<BarberContext>(options =>
        options.UseSqlServer(connectionString));
}

// Register authorization handlers
// AdminHandler requires BarberContext (scoped), so register it as scoped instead of singleton
builder.Services.AddScoped<IAuthorizationHandler, AdminHandler>();
builder.Services.AddScoped<IAuthorizationHandler, BarberOrAdminHandler>();

// Register services
builder.Services.AddScoped<BarberPro.Services.DisponibilidadService>();

// Agregar soporte para sesiones
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Authentication: Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Login";
        options.AccessDeniedPath = "/Login/Login";

        // Development-friendly cookie settings
        options.Cookie.IsEssential = true;
        options.Cookie.HttpOnly = true;
        // Use SecurePolicy Always in production to ensure cookies are secure; SameAsRequest for local development
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Authorization: exigir usuario autenticado por defecto (se permite AllowAnonymous en las acciones públicas)
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("AdminOnly", policy => policy.Requirements.Add(new AdminRequirement()));
    options.AddPolicy("BarberOrAdmin", policy => policy.Requirements.Add(new BarberOrAdminRequirement()));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Habilitar sesiones
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
