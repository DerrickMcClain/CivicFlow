using CivicFlow.Application.Common;
using CivicFlow.Application.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/requests")]
public class RequestsController(RequestService requests) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Citizen")]
    public async Task<ActionResult<ServiceRequestDetailDto>> Create(
        [FromBody] CreateRequestRequest body,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await requests.CreateAsync(
                CurrentUser.GetUserId(User),
                body,
                GetIp(),
                cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = created.RequestId }, created);
        }
        catch (AppException ex)
        {
            return Map(ex);
        }
    }

    [HttpGet("my")]
    [Authorize(Roles = "Citizen")]
    public async Task<ActionResult<IReadOnlyList<ServiceRequestListDto>>> ListMine(
        CancellationToken cancellationToken)
    {
        var items = await requests.ListMineAsync(CurrentUser.GetUserId(User), cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ServiceRequestDetailDto>> Get(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var detail = await requests.GetAsync(
                id,
                CurrentUser.GetUserId(User),
                CurrentUser.GetRole(User),
                cancellationToken);
            return Ok(detail);
        }
        catch (AppException ex)
        {
            return Map(ex);
        }
    }

    [HttpPost("{id:int}/responses")]
    [Authorize(Roles = "Citizen")]
    public async Task<ActionResult<ServiceRequestDetailDto>> Respond(
        int id,
        [FromBody] RespondRequest body,
        CancellationToken cancellationToken)
    {
        try
        {
            var detail = await requests.RespondAsync(
                id,
                CurrentUser.GetUserId(User),
                body.Message,
                GetIp(),
                cancellationToken);
            return Ok(detail);
        }
        catch (AppException ex)
        {
            return Map(ex);
        }
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private ObjectResult Map(AppException ex) => StatusCode(ex.Status, new
    {
        status = ex.Status,
        message = ex.Message,
        traceId = HttpContext.TraceIdentifier
    });
}
