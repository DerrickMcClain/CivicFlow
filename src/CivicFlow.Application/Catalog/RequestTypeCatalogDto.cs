namespace CivicFlow.Application.Catalog;

public sealed class RequestTypeCatalogDto
{
    public int ServiceRequestTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
}
