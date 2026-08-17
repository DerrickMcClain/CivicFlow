using CivicFlow.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CivicFlow.Application.Assistant;

public sealed class PolicyAssistantService(IAppDbContext db)
{
    public async Task<IReadOnlyList<PolicyArticleDto>> SearchAsync(
        string? query,
        CancellationToken cancellationToken = default)
    {
        var normalized = query?.Trim();
        var articles = db.PolicyArticles.AsNoTracking().Where(x => x.IsActive);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return await articles
                .OrderBy(x => x.Title)
                .Take(10)
                .Select(x => new PolicyArticleDto
                {
                    PolicyArticleId = x.PolicyArticleId,
                    Title = x.Title,
                    Summary = x.Summary,
                    Body = x.Body
                })
                .ToListAsync(cancellationToken);
        }

        var pattern = $"%{normalized}%";
        return await articles
            .Where(x =>
                EF.Functions.Like(x.Title, pattern)
                || EF.Functions.Like(x.Summary, pattern)
                || EF.Functions.Like(x.Body, pattern)
                || EF.Functions.Like(x.Keywords, pattern))
            .OrderBy(x => x.Title)
            .Take(10)
            .Select(x => new PolicyArticleDto
            {
                PolicyArticleId = x.PolicyArticleId,
                Title = x.Title,
                Summary = x.Summary,
                Body = x.Body
            })
            .ToListAsync(cancellationToken);
    }
}
