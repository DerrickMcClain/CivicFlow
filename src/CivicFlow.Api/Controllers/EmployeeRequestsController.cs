using CivicFlow.Application.Common;
using CivicFlow.Application.Requests;
using CivicFlow.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicFlow.Api.Controllers;

[Authorize(Roles = "Employee,Supervisor,Administrator")]
[ApiController]
[Route("api/employee/requests")]
public class EmployeeRequestsController(RequestService requests) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceRequestListDto>>> Queue(
        [FromQuery] RequestStatusName? status,
        [FromQuery] Priority? priority,
        CancellationToken cancellationToken)
    {
        try
        {
            var items = await requests.ListStaffQueueAsync(
                CurrentUser.GetRole(User),
                CurrentUser.GetUserId(User),
                status,
                priority,
                cancellationToken);
            return Ok(items);
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

    [HttpGet("assignees")]
    public async Task<ActionResult<IReadOnlyList<StaffAssigneeDto>>> Assignees(
        CancellationToken cancellationToken)
    {
        return Ok(await requests.ListAssigneesAsync(cancellationToken));
    }
}
