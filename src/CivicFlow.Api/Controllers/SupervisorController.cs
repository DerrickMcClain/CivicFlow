using CivicFlow.Application.Common;
using CivicFlow.Application.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicFlow.Api.Controllers;

[Authorize(Roles = "Supervisor,Administrator")]
[ApiController]
[Route("api/supervisor")]
public class SupervisorController(RequestService requests) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<SupervisorDashboardDto>> Dashboard(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await requests.GetSupervisorDashboardAsync(cancellationToken));
        }
        catch (AppException ex)
        {
            return StatusCode(ex.Status, new
            {
                status = ex.Status,
                message = ex.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }
    }
}
