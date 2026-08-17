namespace CivicFlow.Application.Assistant;

public sealed class PolicyArticleDto
{
    public int PolicyArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
