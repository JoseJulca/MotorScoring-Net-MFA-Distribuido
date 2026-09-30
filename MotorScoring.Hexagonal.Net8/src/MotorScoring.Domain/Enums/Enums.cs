namespace MotorScoring.Domain.Enums;

public enum TipoDocumento { DNI, RUC, CE, PASAPORTE }
public enum Moneda { PEN, USD }
public enum EstadoRegistro { ACTIVO, INACTIVO }
public enum EstadoSolicitud { REGISTRADA, EVALUADA, CANCELADA }
public enum EstadoProducto { ACTIVO, INACTIVO }
public enum EstadoModelo { ACTIVO, INACTIVO }
public enum EstadoVersionModelo { ACTIVA, INACTIVA, VENCIDA }
public enum EstadoFactor { ACTIVO, INACTIVO }
public enum EstadoRegla { ACTIVA, INACTIVA }
public enum EstadoEvaluacion { COMPLETADA, CON_REGLA_EXCLUYENTE }
public enum ResultadoScoring { PREAPROBADA, REVISION_MANUAL, RECHAZADA }
