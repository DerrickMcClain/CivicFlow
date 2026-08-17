using CivicFlow.Application.Abstractions;
using CivicFlow.Domain.Workflow;
using Microsoft.EntityFrameworkCore;

namespace CivicFlow.Infrastructure;

public sealed class RequestNumberGenerator(CivicFlowDbContext db) : IRequestNumberGenerator
{
    public async Task<string> NextAsync(CancellationToken cancellationToken = default)
    {
        var next = await db.Database
            .SqlQueryRaw<int>("SELECT NEXT VALUE FOR dbo.ServiceRequestNumberSeq AS [Value]")
            .SingleAsync(cancellationToken);

        return RequestNumberFormatter.Format(DateTime.UtcNow.Year, next);
    }
}
