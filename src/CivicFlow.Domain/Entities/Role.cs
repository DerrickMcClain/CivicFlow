using CivicFlow.Domain.Enums;

namespace CivicFlow.Domain.Entities;

public class Role
{
    public int RoleId { get; set; }
    public RoleName RoleName { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}
