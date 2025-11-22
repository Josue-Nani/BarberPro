using System.Security.Claims;
using BarberPro.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BarberPro.Middleware;

public class EnsureActiveUserMiddleware
{
    private readonly RequestDelegate _next;

    public EnsureActiveUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var logger = context.RequestServices.GetService(typeof(ILogger<EnsureActiveUserMiddleware>)) as ILogger;

        // Excluir rutas públicas para evitar bucles
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/Login", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/favicon.ico", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var idClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            logger?.LogDebug("EnsureActiveUser: Authenticated user idClaim={IdClaim}", idClaim);
            if (int.TryParse(idClaim, out var userId))
            {
                try
                {
                    var db = context.RequestServices.GetRequiredService<BarberContext>();
                    var user = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.UsuarioID == userId);
                    if (user == null || user.Estado != true)
                    {
                        // Si no está activo, cerrar sesión y redirigir a login
                        logger?.LogInformation("EnsureActiveUser: user {UserId} is null or inactive, signing out", userId);
                        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        var loginPath = "/Login/Login";
                        var redirect = loginPath + "?reason=inactive";
                        context.Response.Redirect(redirect);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "EnsureActiveUser: DB error checking user {UserId}", userId);
                    // En caso de fallo de DB, cerrar sesión por seguridad
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Response.Redirect("/Login/Login");
                    return;
                }
            }
        }

        await _next(context);
    }
}
