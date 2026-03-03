using Hecateon.Models;
using Hecateon.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hecateon.Endpoints;

public static class DownshiftEndpoints
{
    public static void MapDownshiftEndpoints(this WebApplication app)
    {
        app.MapPost("/api/downshift/activate", async ([FromServices] DownshiftProtocolService downshiftService, [FromBody] ActivateProtocolRequest request, CancellationToken cancellationToken = default) =>
        {
            try
            {
                var protocol = await downshiftService.ActivateProtocolAsync(request.TriggeringState, cancellationToken);
                return Results.Ok(protocol);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error activating downshift protocol: {ex}");
                return Results.StatusCode(500);
            }
        });

        app.MapPost("/api/downshift/{protocolId}/complete-item", async ([FromServices] DownshiftProtocolService downshiftService, Guid protocolId, [FromBody] CompleteItemRequest request, CancellationToken cancellationToken = default) =>
        {
            try
            {
                var completed = await downshiftService.CompleteChecklistItemAsync(protocolId, request.ItemId, request.Note, cancellationToken);
                return Results.Ok(new { completed });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error completing checklist item: {ex}");
                return Results.StatusCode(500);
            }
        });

        app.MapPost("/api/downshift/{protocolId}/delay-decision", async ([FromServices] DownshiftProtocolService downshiftService, Guid protocolId, [FromBody] DelayDecisionRequest request, CancellationToken cancellationToken = default) =>
        {
            try
            {
                var recorded = await downshiftService.RecordDelayedDecisionAsync(protocolId, request.Description, request.HoursToDelay, cancellationToken);
                return Results.Ok(new { recorded });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error recording delayed decision: {ex}");
                return Results.StatusCode(500);
            }
        });

        app.MapGet("/api/crisis-plan", async ([FromServices] DownshiftProtocolService downshiftService, CancellationToken cancellationToken = default) =>
        {
            try
            {
                var plan = await downshiftService.GetCrisisPlanAsync(cancellationToken);
                return Results.Ok(plan);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error fetching crisis plan: {ex}");
                return Results.StatusCode(500);
            }
        });

        app.MapPost("/api/crisis-plan", async ([FromServices] DownshiftProtocolService downshiftService, [FromBody] CrisisPlan plan, CancellationToken cancellationToken = default) =>
        {
            try
            {
                var saved = await downshiftService.SaveCrisisPlanAsync(plan, cancellationToken);
                return Results.Ok(new { saved });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error saving crisis plan: {ex}");
                return Results.StatusCode(500);
            }
        });
    }
}
