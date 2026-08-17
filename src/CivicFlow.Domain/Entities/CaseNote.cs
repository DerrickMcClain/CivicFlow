namespace CivicFlow.Domain.Entities;

public class CaseNote
{
    public int NoteId { get; set; }
    public int RequestId { get; set; }
    public int AuthorId { get; set; }
    public string NoteText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsInternal { get; set; }
    public ServiceRequest Request { get; set; } = null!;
    public User Author { get; set; } = null!;
}
