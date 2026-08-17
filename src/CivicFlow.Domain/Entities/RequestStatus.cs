using CivicFlow.Domain.Enums;

namespace CivicFlow.Domain.Entities;

public class RequestStatus
{
    public int StatusId { get; set; }
    public RequestStatusName StatusName { get; set; }
    public bool IsTerminal { get; set; }
}
