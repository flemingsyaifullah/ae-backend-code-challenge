using AeShipManagement.Application.FinancialReports;
using Microsoft.AspNetCore.Mvc;

namespace AeShipManagement.Api.Controllers;

[ApiController]
[Route("api/v1/ships/{shipCode}/financial-reports")]
[Produces("application/json")]
[Tags("Financial reports")]
public sealed class FinancialReportsController(IFinancialReportService reportService) : ControllerBase
{
    [HttpGet("detail")]
    [EndpointSummary("Detailed expense report (all account levels) for a ship and period")]
    [EndpointDescription(
        "period: yyyy-MM. Returns actual, budget and variance (actual - budget) for the period and year-to-date " +
        "based on the ship's fiscal year. Parent accounts aggregate their child accounts. Null means no data recorded.")]
    [ProducesResponseType<FinancialReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<FinancialReportResponse> Detail(string shipCode, [FromQuery] string? period, CancellationToken cancellationToken) =>
        reportService.GetAsync(new FinancialReportQuery(shipCode, period, FinancialReportType.Detail), cancellationToken);

    [HttpGet("summary")]
    [EndpointSummary("Summary expense report (parent accounts only) for a ship and period")]
    [ProducesResponseType<FinancialReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<FinancialReportResponse> Summary(string shipCode, [FromQuery] string? period, CancellationToken cancellationToken) =>
        reportService.GetAsync(new FinancialReportQuery(shipCode, period, FinancialReportType.Summary), cancellationToken);
}
