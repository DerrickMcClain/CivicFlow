namespace CivicFlow.Domain.Entities;

public class Department
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<ServiceRequestType> RequestTypes { get; set; } = new List<ServiceRequestType>();
}
