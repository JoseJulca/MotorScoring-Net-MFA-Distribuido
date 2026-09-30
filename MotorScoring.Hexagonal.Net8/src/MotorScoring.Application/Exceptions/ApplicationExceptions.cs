namespace MotorScoring.Application.Exceptions;

public sealed class RecursoNoEncontradoException(string message) : Exception(message);
public sealed class SolicitudDuplicadaException(string message) : Exception(message);
