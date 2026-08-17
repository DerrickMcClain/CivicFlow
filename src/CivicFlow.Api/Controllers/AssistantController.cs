using CivicFlow.Application.Assistant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/assistant")]
public class AssistantController(PolicyAssistantService assistant) : ControllerBase
{
    [HttpGet("policies")]
    public async Task<ActionResult<IReadOnlyList<PolicyArticleDto>>> Search(
        [FromQuery] string? query,
        CancellationToken cancellationToken)
    {
        return Ok(await assistant.SearchAsync(query, cancellationToken));
    }
}
