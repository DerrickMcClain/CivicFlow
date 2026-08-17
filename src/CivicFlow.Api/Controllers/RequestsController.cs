using CivicFlow.Application.Common;
using CivicFlow.Application.Documents;
using CivicFlow.Application.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/requests")]
public class RequestsController(RequestService requests, DocumentService documents) : ControllerBase
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

    [HttpPost("{id:int}/documents")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<DocumentDto>> UploadDocument(
        int id,
        IFormFile file,
        [FromForm] bool isInternal = false,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { status = 400, message = "A file is required." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var uploaded = await documents.UploadAsync(
                id,
                CurrentUser.GetUserId(User),
                CurrentUser.GetRole(User),
                file.FileName,
                file.ContentType,
                file.Length,
                stream,
                isInternal,
                GetIp(),
                cancellationToken);
            return CreatedAtAction(nameof(DownloadDocument), new { id, documentId = uploaded.DocumentId }, uploaded);
        }
        catch (AppException ex)
        {
            return Map(ex);
        }
    }

    [HttpGet("{id:int}/documents/{documentId:int}")]
    public async Task<IActionResult> DownloadDocument(
        int id,
        int documentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var download = await documents.DownloadAsync(
                id,
                documentId,
                CurrentUser.GetUserId(User),
                CurrentUser.GetRole(User),
                cancellationToken);
            return File(download.Content, download.ContentType, download.FileName);
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

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Employee,Supervisor,Administrator")]
    public async Task<ActionResult<ServiceRequestDetailDto>> ChangeStatus(
        int id,
        [FromBody] ChangeStatusRequest body,
        CancellationToken cancellationToken)
    {
        try
        {
            var detail = await requests.ChangeStatusAsync(
                id,
                CurrentUser.GetUserId(User),
                CurrentUser.GetRole(User),
                body.Status,
                body.Reason,
                GetIp(),
                cancellationToken);
            return Ok(detail);
        }
        catch (AppException ex)
        {
            return Map(ex);
        }
    }

    [HttpPost("{id:int}/notes")]
    [Authorize(Roles = "Employee,Supervisor,Administrator")]
    public async Task<ActionResult<ServiceRequestDetailDto>> AddNote(
        int id,
        [FromBody] AddNoteRequest body,
        CancellationToken cancellationToken)
    {
        try
        {
            var detail = await requests.AddNoteAsync(
                id,
                CurrentUser.GetUserId(User),
                CurrentUser.GetRole(User),
                body.NoteText,
                body.IsInternal,
                cancellationToken);
            return Ok(detail);
        }
        catch (AppException ex)
        {
            return Map(ex);
        }
    }

    [HttpPut("{id:int}/assignment")]
    [Authorize(Roles = "Employee,Supervisor,Administrator")]
    public async Task<ActionResult<ServiceRequestDetailDto>> Assign(
        int id,
        [FromBody] AssignRequest body,
        CancellationToken cancellationToken)
    {
        try
        {
            var detail = await requests.AssignAsync(
                id,
                CurrentUser.GetUserId(User),
                CurrentUser.GetRole(User),
                body.AssignedToUserId,
                body.Reason,
                GetIp(),
                cancellationToken);
            return Ok(detail);
        }
        catch (AppException ex)
        {
            return Map(ex);
        }
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "Supervisor")]
    public async Task<ActionResult<ServiceRequestDetailDto>> Approve(
        int id,
        [FromBody] DecisionRequest? body,
        CancellationToken cancellationToken)
    {
        try
        {
            var detail = await requests.ApproveAsync(
                id,
                CurrentUser.GetUserId(User),
                CurrentUser.GetRole(User),
                body?.Reason,
                GetIp(),
                cancellationToken);
            return Ok(detail);
        }
        catch (AppException ex)
        {
            return Map(ex);
        }
    }

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "Supervisor")]
    public async Task<ActionResult<ServiceRequestDetailDto>> Reject(
        int id,
        [FromBody] DecisionRequest body,
        CancellationToken cancellationToken)
    {
        try
        {
            var detail = await requests.RejectAsync(
                id,
                CurrentUser.GetUserId(User),
                CurrentUser.GetRole(User),
                body.Reason,
                GetIp(),
                cancellationToken);
            return Ok(detail);
        }
        catch (AppException ex)
        {
            return Map(ex);
        }
    }

    [HttpPut("{id:int}/reassign")]
    [Authorize(Roles = "Supervisor,Administrator")]
    public async Task<ActionResult<ServiceRequestDetailDto>> Reassign(
        int id,
        [FromBody] AssignRequest body,
        CancellationToken cancellationToken)
    {
        try
        {
            var detail = await requests.ReassignAsync(
                id,
                CurrentUser.GetUserId(User),
                CurrentUser.GetRole(User),
                body.AssignedToUserId,
                body.Reason,
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
