using System.Data;
using CivicFlow.Application.Abstractions;
using CivicFlow.Domain.Workflow;
using Microsoft.EntityFrameworkCore;

namespace CivicFlow.Infrastructure;

public sealed class RequestNumberGenerator(CivicFlowDbContext db) : IRequestNumberGenerator
{
    public async Task<string> NextAsync(CancellationToken cancellationToken = default)
    {
        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT NEXT VALUE FOR dbo.ServiceRequestNumberSeq";
            var result = await command.ExecuteScalarAsync(cancellationToken);
            var sequence = Convert.ToInt32(result);
            return RequestNumberFormatter.Format(DateTime.UtcNow.Year, sequence);
        }
        finally
        {
            if (openedHere)
            {
                await db.Database.CloseConnectionAsync();
            }
        }
    }
}
