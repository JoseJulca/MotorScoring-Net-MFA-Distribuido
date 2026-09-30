using MotorScoring.Identity.Domain.Entities;

namespace MotorScoring.Identity.Application.Models;

public enum AuthError
{
    None,
    Forbidden,
    Conflict,
    BadRequest,
    Unauthorized,
    NotFound
}

public sealed record OperationResult<T>(bool Succeeded, T? Value, AuthError Error, string? Message = null, IReadOnlyList<string>? Errors = null)
{
    public static OperationResult<T> Ok(T value) => new(true, value, AuthError.None);
    public static OperationResult<T> Fail(AuthError error, string message, IReadOnlyList<string>? errors = null) => new(false, default, error, message, errors);
}

public sealed record RegisteredUser(Guid Id, string? Email, string? DisplayName);
public sealed record ExternalUserInput(string Provider, string ProviderKey, string? Email, string? DisplayName);
public sealed record ExternalLoginResult(UserAccount User, string ExchangeCode);
