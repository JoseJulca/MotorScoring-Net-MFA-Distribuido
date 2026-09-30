namespace MotorScoring.Web.Models.Responses;

public sealed record ApiErrorResponse(
    DateTimeOffset Timestamp,
    int Status,
    string Code,
    string Message,
    string Path,
    object? Validation = null);
