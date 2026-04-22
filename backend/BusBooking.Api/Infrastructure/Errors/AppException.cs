namespace BusBooking.Api.Infrastructure.Errors;

public abstract class AppException : Exception
{
    protected AppException(string code, string message, int httpStatus, object? details = null) : base(message)
    {
        Code = code;
        HttpStatus = httpStatus;
        Details = details;
    }

    public string Code { get; }
    public int HttpStatus { get; }
    public object? Details { get; }
}

public class NotFoundException(string message) : AppException("NOT_FOUND", message, 404);
public class ConflictException(string code, string message, object? details = null) : AppException(code, message, 409, details);
public class BusinessRuleException(string code, string message, object? details = null) : AppException(code, message, 422, details);
public class ForbiddenException(string message = "Forbidden") : AppException("FORBIDDEN", message, 403);
public class UnauthorizedException(string code = "UNAUTHORIZED", string message = "Unauthorized") : AppException(code, message, 401);
