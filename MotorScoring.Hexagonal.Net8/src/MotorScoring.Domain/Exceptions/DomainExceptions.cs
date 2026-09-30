namespace MotorScoring.Domain.Exceptions;

public class DomainException(string message) : Exception(message);
public sealed class SolicitudNoEvaluableException(string message) : DomainException(message);
public sealed class PuntajeInvalidoException(string message) : DomainException(message);
public sealed class ModeloActivoNoEncontradoException(string message) : DomainException(message);
