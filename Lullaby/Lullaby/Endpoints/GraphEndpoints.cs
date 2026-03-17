using Hecateon.Data;
using Hecateon.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hecateon.Endpoints;

public static class GraphEndpoints
{
    public static void MapGraphEndpoints(this WebApplication app)
    {
        app.MapGet("/api/graph/status", async (
            HttpContext http,
            [FromServices] IGraphProjectionService graphProjection,
            [FromServices] ChatDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var lastAppliedSeq = await graphProjection.GetLastAppliedSeqAsync(cancellationToken);
            var nodeCount = await dbContext.GraphNodes.CountAsync(cancellationToken);
            var edgeCount = await dbContext.GraphEdges.CountAsync(cancellationToken);
            var evidenceCount = await dbContext.GraphEvidence.CountAsync(cancellationToken);

            return Results.Ok(new
            {
                lastAppliedSeq,
                counts = new
                {
                    nodes = nodeCount,
                    edges = edgeCount,
                    evidence = evidenceCount
                },
                correlationId = http.TraceIdentifier
            });
        });

        app.MapPost("/api/graph/apply", async (
            HttpContext http,
            [FromServices] IGraphProjectionService graphProjection,
            [FromServices] ILogger<Program> logger,
            [FromQuery] int batchSize = 200,
            CancellationToken cancellationToken = default) =>
        {
            try
            {
                var result = await graphProjection.ApplyPendingAsync(batchSize, cancellationToken);
                logger.LogInformation(
                    "Graph projection apply completed. Applied={Applied} LastSeq={LastSeq} CorrelationId={CorrelationId}",
                    result.AppliedEvents,
                    result.LastAppliedSeq,
                    http.TraceIdentifier);

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Graph projection apply failed. CorrelationId={CorrelationId}", http.TraceIdentifier);
                return Problem(http, StatusCodes.Status500InternalServerError, "Graph apply failed", "An unexpected error occurred during graph projection apply.", "https://hecateon.dev/problems/graph-apply-failed");
            }
        });

        app.MapPost("/api/graph/rebuild", async (
            HttpContext http,
            [FromServices] IGraphProjectionService graphProjection,
            [FromServices] ILogger<Program> logger,
            [FromQuery] int batchSize = 200,
            CancellationToken cancellationToken = default) =>
        {
            try
            {
                var result = await graphProjection.RebuildAsync(batchSize, cancellationToken);
                logger.LogInformation(
                    "Graph projection rebuild completed. Applied={Applied} LastSeq={LastSeq} CorrelationId={CorrelationId}",
                    result.AppliedEvents,
                    result.LastAppliedSeq,
                    http.TraceIdentifier);

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Graph projection rebuild failed. CorrelationId={CorrelationId}", http.TraceIdentifier);
                return Problem(http, StatusCodes.Status500InternalServerError, "Graph rebuild failed", "An unexpected error occurred during graph projection rebuild.", "https://hecateon.dev/problems/graph-rebuild-failed");
            }
        });
    }

    private static IResult Problem(HttpContext http, int statusCode, string title, string detail, string type)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type,
            Instance = http.Request.Path
        };

        problem.Extensions["correlationId"] = http.TraceIdentifier;
        return Results.Problem(problem);
    }
}
