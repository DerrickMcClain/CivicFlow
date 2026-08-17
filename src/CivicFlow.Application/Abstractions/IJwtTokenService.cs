using CivicFlow.Domain.Entities;

namespace CivicFlow.Application.Abstractions;

public interface IJwtTokenService
{
    string CreateToken(User user);
}
