namespace CivicFlow.Application.Common;

public abstract class AppException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

public sealed class NotFoundException(string message) : AppException(404, message);

public sealed class ForbiddenException(string message) : AppException(403, message);

public sealed class ConflictException(string message) : AppException(409, message);

public sealed class ValidationException(string message) : AppException(400, message);

public sealed class UnauthorizedException(string message) : AppException(401, message);
