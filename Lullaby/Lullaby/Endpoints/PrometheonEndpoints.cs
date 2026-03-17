using Hecateon.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hecateon.Endpoints;

public static class PrometheonEndpoints
{
    public static void MapPrometheonEndpoints(this WebApplication app)
    {
        app.MapGet("/api/prometheon/status", async (
            HttpContext http,
            [FromServices] IPrometheonExtractionService extraction,
            CancellationToken cancellationToken) =>
        {
            var lastProcessedSeq = await extraction.GetLastProcessedSeqAsync(cancellationToken);
            return Results.Ok(new
            {
                lastProcessedSeq,
                correlationId = http.TraceIdentifier
            });
        });

        app.MapPost("/api/prometheon/extract", async (
            HttpContext http,
            [FromServices] IPrometheonExtractionService extraction,
            [FromServices] ILogger<Program> logger,
            [FromQuery] int batchSize = 100,
            CancellationToken cancellationToken = default) =>
        {
            try
            {
                var result = await extraction.ProcessPendingChatEventsAsync(batchSize, cancellationToken);
                logger.LogInformation(
                    "Prometheon extraction completed. ProcessedChat={ProcessedChat} EmittedGraph={EmittedGraph} LastSeq={LastSeq} CorrelationId={CorrelationId}",
                    result.ProcessedChatEvents,
                    result.EmittedGraphEvents,
                    result.LastProcessedSeq,
                    http.TraceIdentifier);

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Prometheon extraction failed. CorrelationId={CorrelationId}", http.TraceIdentifier);
                return Problem(http, StatusCodes.Status500InternalServerError, "Prometheon extraction failed", "An unexpected error occurred while extracting graph events from chat stream.", "https://hecateon.dev/problems/prometheon-extract-failed");
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
