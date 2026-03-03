using Hecateon.Core.DeviceRegistry;
using Hecateon.Core.EventStore;
using Hecateon.Models.Api.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Hecateon.Endpoints;

public static class HecateonCoreEndpoints
{
    public static void MapHecateonCoreEndpoints(this WebApplication app)
    {
        app.MapPost("/api/hecateon/device/enroll", async ([FromServices] IDeviceRegistry deviceRegistry, [FromBody] EnrollDeviceRequest request, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.DeviceId))
                return Results.BadRequest(new { error = "DeviceId is required" });

            var device = await deviceRegistry.EnrollAsync(request.DeviceId, request.DisplayName, cancellationToken);
            return Results.Ok(new
            {
                deviceId = device.DeviceId,
                displayName = device.DisplayName,
                isApproved = device.IsApproved,
                enrolledUtc = device.EnrolledUtc
            });
        });

        app.MapPost("/api/hecateon/device/approve", async ([FromServices] IDeviceRegistry deviceRegistry, HttpContext http, IConfiguration configuration, [FromBody] ApproveDeviceRequest request, CancellationToken cancellationToken) =>
        {
            var recoveryCode = configuration["Security:RecoveryCode"] ?? "CHANGE_THIS_RECOVERY_CODE";
            var suppliedCode = http.Request.Headers["X-Recovery-Code"].FirstOrDefault();
            if (!string.Equals(recoveryCode, suppliedCode, StringComparison.Ordinal))
                return Results.Unauthorized();

            var device = await deviceRegistry.ApproveAsync(request.DeviceId, request.Scopes, cancellationToken);
            return device != null
                ? Results.Ok(new { deviceId = device.DeviceId, approved = device.IsApproved, scopes = device.Scopes })
                : Results.NotFound();
        });

        app.MapPost("/api/hecateon/device/revoke", async ([FromServices] IDeviceRegistry deviceRegistry, HttpContext http, IConfiguration configuration, [FromBody] RevokeDeviceRequest request, CancellationToken cancellationToken) =>
        {
            var recoveryCode = configuration["Security:RecoveryCode"] ?? "CHANGE_THIS_RECOVERY_CODE";
            var suppliedCode = http.Request.Headers["X-Recovery-Code"].FirstOrDefault();
            if (!string.Equals(recoveryCode, suppliedCode, StringComparison.Ordinal))
                return Results.Unauthorized();

            var revoked = await deviceRegistry.RevokeAsync(request.DeviceId, cancellationToken);
            return revoked ? Results.Ok(new { revoked = true }) : Results.NotFound();
        });

        app.MapGet("/api/hecateon/devices", async ([FromServices] IDeviceRegistry deviceRegistry, HttpContext http, IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            var recoveryCode = configuration["Security:RecoveryCode"] ?? "CHANGE_THIS_RECOVERY_CODE";
            var suppliedCode = http.Request.Headers["X-Recovery-Code"].FirstOrDefault();
            if (!string.Equals(recoveryCode, suppliedCode, StringComparison.Ordinal))
                return Results.Unauthorized();

            var devices = await deviceRegistry.GetAllDevicesAsync(cancellationToken);
            return Results.Ok(devices.Select(d => new
            {
                d.DeviceId,
                d.DisplayName,
                d.IsApproved,
                d.Scopes,
                d.EnrolledUtc,
                d.ApprovedUtc,
                d.LastSeenUtc
            }));
        });

        app.MapGet("/api/hecateon/events", async ([FromServices] IEventStore eventStore, CancellationToken cancellationToken, [FromQuery] int skip = 0, [FromQuery] int take = 100) =>
        {
            var events = await eventStore.GetAllEventsAsync(skip, take, cancellationToken);
            return Results.Ok(events);
        });
    }
}
