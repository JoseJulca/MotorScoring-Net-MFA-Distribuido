namespace MotorScoring.Identity.Domain.Entities;

public sealed record UserSummary(
    Guid Id,
    string? Email,
    string? DisplayName,
    bool TwoFactorEnabled,
    DateTimeOffset? LockoutEnd);
