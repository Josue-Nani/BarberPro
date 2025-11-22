using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;

namespace BarberPro.Middleware;

public class RequireAuthenticatedMiddleware
{
    private readonly RequestDelegate _next;

    public RequireAuthenticatedMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var logger = context.RequestServices.GetService(typeof(ILogger<RequireAuthenticatedMiddleware>)) as ILogger;
        var path = context.Request.Path.Value ?? string.Empty;

        // Exclude public paths
        if (path.StartsWith("/Login", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/favicon.ico", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/Home/Error", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Log some info for debugging
        try
        {
            logger?.LogDebug("RequireAuthMiddleware: Path={Path}, IsAuthenticated={IsAuthenticated}", path, context.User?.Identity?.IsAuthenticated == true);
            if (context.Request.Cookies != null)
            {
                var cookieNames = string.Join(',', context.Request.Cookies.Keys);
                logger?.LogDebug("RequireAuthMiddleware: Cookies present={Cookies}", cookieNames);
            }
        }
        catch { }

        // If user is not authenticated, redirect to login
        if (!(context.User?.Identity?.IsAuthenticated == true))
        {
            var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
            logger?.LogInformation("RequireAuthMiddleware: redirecting to login, returnUrl={ReturnUrl}", returnUrl);
            context.Response.Redirect($"/Login/Login?returnUrl={returnUrl}");
            return;
        }

        await _next(context);
    }
}
