namespace MotorScoring.Identity.Domain.Entities;

public sealed record UserAccount(
    Guid Id,
    string Email,
    string? DisplayName,
    bool MfaEnabled,
    DateTimeOffset? LockoutEnd = null);
