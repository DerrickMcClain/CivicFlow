using CivicFlow.Application.Auth;
using CivicFlow.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await auth.LoginAsync(request, cancellationToken));
        }
        catch (UnauthorizedException)
        {
            return Unauthorized();
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthResponse>> Me(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await auth.GetCurrentAsync(CurrentUser.GetUserId(User), cancellationToken));
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

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await auth.RegisterCitizenAsync(request, cancellationToken));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { status = 400, message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { status = 409, message = ex.Message });
        }
    }
}
