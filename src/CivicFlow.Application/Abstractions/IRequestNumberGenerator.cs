namespace CivicFlow.Application.Abstractions;

public interface IRequestNumberGenerator
{
    Task<string> NextAsync(CancellationToken cancellationToken = default);
}
