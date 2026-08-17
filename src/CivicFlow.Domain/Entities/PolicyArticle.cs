namespace CivicFlow.Domain.Entities;

public class PolicyArticle
{
    public int PolicyArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Keywords { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
