using Lullaby.Models;
using Lullaby.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lullaby.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this WebApplication app)
    {
        app.MapGet("/api/reports/weekly", async (HttpContext http, [FromServices] WeeklyReportService reportService, IConfiguration configuration, [FromQuery] string? format = "markdown", CancellationToken cancellationToken = default) =>
        {
            try
            {
                var recoveryCode = configuration["Security:RecoveryCode"] ?? "CHANGE_THIS_RECOVERY_CODE";
                var suppliedCode = http.Request.Headers["X-Recovery-Code"].FirstOrDefault();
                if (!string.Equals(recoveryCode, suppliedCode, StringComparison.Ordinal))
                {
                    return Results.Unauthorized();
                }

                var report = await reportService.GenerateWeeklyReportAsync(null, cancellationToken);

                var exportFormat = Enum.TryParse<ExportFormat>(format, true, out var fmt) ? fmt : ExportFormat.Markdown;
                var filePath = await reportService.ExportReportAsync(report, exportFormat, cancellationToken);

                return Results.Ok(new { reportId = report.ReportId, filePath, format = exportFormat.ToString() });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error generating report: {ex}");
                return Results.StatusCode(500);
            }
        });

        app.MapGet("/api/reports/weekly/preview", async (HttpContext http, [FromServices] WeeklyReportService reportService, IConfiguration configuration, CancellationToken cancellationToken = default) =>
        {
            try
            {
                var recoveryCode = configuration["Security:RecoveryCode"] ?? "CHANGE_THIS_RECOVERY_CODE";
                var suppliedCode = http.Request.Headers["X-Recovery-Code"].FirstOrDefault();
                if (!string.Equals(recoveryCode, suppliedCode, StringComparison.Ordinal))
                {
                    return Results.Unauthorized();
                }

                var report = await reportService.GenerateWeeklyReportAsync(null, cancellationToken);
                return Results.Ok(report);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error generating report preview: {ex}");
                return Results.StatusCode(500);
            }
        });
    }
}
