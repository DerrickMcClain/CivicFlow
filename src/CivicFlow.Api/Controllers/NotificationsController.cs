using CivicFlow.Application.Common;
using CivicFlow.Application.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/notifications")]
public class NotificationsController(NotificationService notifications) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await notifications.ListMineAsync(CurrentUser.GetUserId(User), cancellationToken));
    }

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken cancellationToken)
    {
        try
        {
            await notifications.MarkReadAsync(CurrentUser.GetUserId(User), id, cancellationToken);
            return NoContent();
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

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await notifications.MarkAllReadAsync(CurrentUser.GetUserId(User), cancellationToken);
        return NoContent();
    }
}
