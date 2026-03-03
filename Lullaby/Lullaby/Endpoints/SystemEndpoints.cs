namespace Lullaby.Endpoints;

public static class SystemEndpoints
{
    public static void MapSystemEndpoints(this WebApplication app)
    {
        app.MapGet("/api/test", () =>
        {
            return Results.Ok(new { message = "API is reachable (client-side processing mode)", timestamp = DateTime.UtcNow });
        });
    }
}
