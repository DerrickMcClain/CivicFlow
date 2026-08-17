using CivicFlow.Application.Admin;
using CivicFlow.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicFlow.Api.Controllers;

[Authorize(Roles = "Administrator")]
[ApiController]
[Route("api/admin")]
public class AdminController(AdminService admin) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<AdminUserDto>>> Users(CancellationToken cancellationToken)
    {
        return Ok(await admin.ListUsersAsync(cancellationToken));
    }

    [HttpPut("users/{id:int}/role")]
    public async Task<ActionResult<AdminUserDto>> UpdateRole(
        int id,
        [FromBody] UpdateUserRoleRequest body,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await admin.UpdateUserRoleAsync(id, body, cancellationToken));
        }
        catch (AppException ex)
        {
            return Map(ex);
        }
    }

    [HttpGet("departments")]
    public async Task<ActionResult<IReadOnlyList<DepartmentDto>>> Departments(
        CancellationToken cancellationToken)
    {
        return Ok(await admin.ListDepartmentsAsync(cancellationToken));
    }

    [HttpPost("departments")]
    public async Task<ActionResult<DepartmentDto>> CreateDepartment(
        [FromBody] UpsertDepartmentRequest body,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await admin.CreateDepartmentAsync(body, cancellationToken);
            return Created($"/api/admin/departments/{created.DepartmentId}", created);
        }
        catch (AppException ex)
        {
            return Map(ex);
        }
    }

    [HttpPut("departments/{id:int}")]
    public async Task<ActionResult<DepartmentDto>> UpdateDepartment(
        int id,
        [FromBody] UpsertDepartmentRequest body,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await admin.UpdateDepartmentAsync(id, body, cancellationToken));
        }
        catch (AppException ex)
        {
            return Map(ex);
        }
    }

    [HttpGet("request-types")]
    public async Task<ActionResult<IReadOnlyList<RequestTypeDto>>> RequestTypes(
        CancellationToken cancellationToken)
    {
        return Ok(await admin.ListRequestTypesAsync(cancellationToken));
    }

    [HttpPost("request-types")]
    public async Task<ActionResult<RequestTypeDto>> CreateRequestType(
        [FromBody] UpsertRequestTypeRequest body,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await admin.CreateRequestTypeAsync(body, cancellationToken);
            return Created($"/api/admin/request-types/{created.ServiceRequestTypeId}", created);
        }
        catch (AppException ex)
        {
            return Map(ex);
        }
    }

    [HttpPut("request-types/{id:int}")]
    public async Task<ActionResult<RequestTypeDto>> UpdateRequestType(
        int id,
        [FromBody] UpsertRequestTypeRequest body,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await admin.UpdateRequestTypeAsync(id, body, cancellationToken));
        }
        catch (AppException ex)
        {
            return Map(ex);
        }
    }

    [HttpGet("audit-logs")]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> AuditLogs(
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        return Ok(await admin.ListAuditLogsAsync(take, cancellationToken));
    }

    private ObjectResult Map(AppException ex) => StatusCode(ex.Status, new
    {
        status = ex.Status,
        message = ex.Message,
        traceId = HttpContext.TraceIdentifier
    });
}
