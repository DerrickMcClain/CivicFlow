namespace CivicFlow.Domain.Entities;

public class ServiceRequestType
{
    public int ServiceRequestTypeId { get; set; }
    public int DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Department Department { get; set; } = null!;
}
