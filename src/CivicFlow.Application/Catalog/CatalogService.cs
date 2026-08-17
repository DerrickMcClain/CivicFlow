using CivicFlow.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CivicFlow.Application.Catalog;

public sealed class CatalogService(IAppDbContext db)
{
    public async Task<IReadOnlyList<RequestTypeCatalogDto>> ListActiveRequestTypesAsync(
        CancellationToken cancellationToken = default)
    {
        return await db.ServiceRequestTypes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new RequestTypeCatalogDto
            {
                ServiceRequestTypeId = x.ServiceRequestTypeId,
                Name = x.Name,
                Description = x.Description,
                DepartmentName = x.Department.DepartmentName
            })
            .ToListAsync(cancellationToken);
    }
}
