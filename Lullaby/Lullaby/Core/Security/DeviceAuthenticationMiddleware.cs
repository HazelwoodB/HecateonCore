namespace Hecateon.Core.Security;

using Hecateon.Core.DeviceRegistry;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Middleware for device authentication and scope validation.
/// Enforces trusted device registry and scope-based access control.
/// </summary>
public class DeviceAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly string[] PublicEndpoints = new[]
    {
        "/api/test",
        "/api/hecateon/device/enroll",
        "/health",
        "/api/health"
    };

    public DeviceAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IDeviceRegistry deviceRegistry, IConfiguration configuration)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip auth for public endpoints and static assets
        if (IsPublicEndpoint(path) || path.StartsWith("/css") || path.StartsWith("/js") || path.StartsWith("/_framework"))
        {
            await _next(context);
            return;
        }

        // For admin-only endpoints (recovery code required)
        if (RequiresRecoveryCode(path))
        {
            var recoveryCode = configuration["Security:RecoveryCode"] ?? "CHANGE_THIS_RECOVERY_CODE";
            var suppliedCode = context.Request.Headers["X-Recovery-Code"].FirstOrDefault();

            if (!string.Equals(recoveryCode, suppliedCode, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Recovery code required");
                return;
            }
        }

        // For device-specific endpoints (device auth required)
        else if (RequiresDeviceAuth(path))
        {
            var deviceId = context.Request.Headers["X-Device-Id"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Device ID required (X-Device-Id header)");
                return;
            }

            // Check if device is approved
            var isApproved = await deviceRegistry.IsApprovedAsync(deviceId, context.RequestAborted);
            if (!isApproved)
            {
                // Enroll or update pending status
                await deviceRegistry.EnrollAsync(deviceId, $"Device-{deviceId}", context.RequestAborted);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Device pending approval");
                return;
            }

            // Check scopes for specific endpoints
            var requiredScope = GetRequiredScope(path);
            if (!string.IsNullOrEmpty(requiredScope))
            {
                var device = await deviceRegistry.GetDeviceAsync(deviceId, context.RequestAborted);
                if (device == null || !device.Scopes.Contains(requiredScope))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync($"Scope required: {requiredScope}");
                    return;
                }
            }
        }

        await _next(context);
    }

    private bool IsPublicEndpoint(string path)
    {
        return PublicEndpoints.Any(ep => path.StartsWith(ep, StringComparison.OrdinalIgnoreCase));
    }

    private bool RequiresRecoveryCode(string path)
    {
        return path.StartsWith("/api/hecateon/device/approve") ||
               path.StartsWith("/api/hecateon/device/revoke") ||
               path.StartsWith("/api/hecateon/devices") ||
               path.StartsWith("/api/reports/weekly") ||
               path.StartsWith("/api/devices");
    }

    private bool RequiresDeviceAuth(string path)
    {
        return path.StartsWith("/api/chat") ||
               path.StartsWith("/api/health") ||
               path.StartsWith("/api/nyphos") ||
               path.StartsWith("/api/sync") ||
             path.StartsWith("/api/graph") ||
             path.StartsWith("/api/prometheon") ||
               path.StartsWith("/api/modes") ||
               path.StartsWith("/api/operator") ||
               path.StartsWith("/api/messages") ||
               path.StartsWith("/api/history") ||
               path.StartsWith("/api/downshift");
    }

    private string GetRequiredScope(string path)
    {
        if (path.StartsWith("/api/health/admin")) return "health:admin";
        if (path.StartsWith("/api/nyphos/admin")) return "nyphos:admin";
        return string.Empty;
    }
}

/// <summary>
/// Extension methods for adding device authentication middleware.
/// </summary>
public static class DeviceAuthenticationExtensions
{
    public static IApplicationBuilder UseDeviceAuthentication(this IApplicationBuilder app)
    {
        return app.UseMiddleware<DeviceAuthenticationMiddleware>();
    }
}
