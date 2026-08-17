using CivicFlow.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/catalog")]
public class CatalogController(CatalogService catalog) : ControllerBase
{
    [HttpGet("request-types")]
    public async Task<ActionResult<IReadOnlyList<RequestTypeCatalogDto>>> RequestTypes(
        CancellationToken cancellationToken)
    {
        return Ok(await catalog.ListActiveRequestTypesAsync(cancellationToken));
    }
}
