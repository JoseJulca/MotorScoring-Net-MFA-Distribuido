namespace MotorScoring.Identity.Security;

public static class SecurityConstants
{
    public const string PermissionClaim = "permission";
    public const string MfaClaim = "mfa";
    public const string PurposeClaim = "purpose";

    public static class Permissions
    {
        public const string ScoringRegistrar = "Scoring.Solicitud.Crear";
        public const string ScoringEvaluar = "Scoring.Evaluacion.Ejecutar";
    }
}
