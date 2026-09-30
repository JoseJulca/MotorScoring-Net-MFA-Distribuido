# Motor de Scoring Crediticio — Arquitectura Hexagonal .NET 8

Implementación en **.NET 8** del Motor de Scoring Crediticio aplicando **Arquitectura Hexagonal (Ports & Adapters)** y conservando las reglas funcionales principales del proyecto original.

Implementa los requerimientos:

- **RF04:** registrar una solicitud de crédito.
- **RF05:** validar la información antes del cálculo.
- **RF06:** calcular el scoring crediticio y persistir el resultado y detalle por factor.

Tecnologías principales: **.NET 8, ASP.NET Core Web API, Entity Framework Core, SQL Server, DbUp, Swagger/OpenAPI, xUnit y Docker**.

## Arquitectura Hexagonal

La solución separa el núcleo funcional de los mecanismos externos mediante puertos y adaptadores.

```text
MotorScoring.Hexagonal.Net8/
├── src/
│   ├── MotorScoring.Domain/
│   │   # Entidades, Value Objects, enums, excepciones
│   │   # y servicios con las reglas de negocio del scoring
│   │
│   ├── MotorScoring.Application/
│   │   # Casos de uso, modelos y puertos de entrada/salida
│   │
│   ├── MotorScoring.Adapters.Inbound.Api/
│   │   # Adaptador de entrada REST
│   │   # Controllers, Contracts, Middleware y configuración
│   │
│   ├── MotorScoring.Adapters.Outbound.Persistence/
│   │   # Adaptador de salida
│   │   # EF Core, repositorios y persistencia SQL Server
│   │
│   └── MotorScoring.Api/
│       # Host ASP.NET Core y Composition Root
│       # Program.cs, configuración, DbUp, Health Check y Docker
│
└── tests/
    └── MotorScoring.Domain.Tests/
        # Pruebas del dominio
```

La dirección conceptual de las dependencias es:

```text
                 Adaptadores de entrada
                  ASP.NET Core REST API
                          │
                          ▼
                  Puertos de entrada
                          │
                          ▼
                  ┌───────────────┐
                  │  Application  │
                  │  Casos de uso │
                  └───────┬───────┘
                          │
                          ▼
                  ┌───────────────┐
                  │    Domain     │
                  │ Reglas negocio│
                  └───────────────┘
                          ▲
                          │
                   Puertos de salida
                          ▲
                          │
                 Adaptadores de salida
                  EF Core / SQL Server
```

El **Domain** contiene las reglas del scoring y no depende de ASP.NET Core, Entity Framework Core ni SQL Server.

`MotorScoring.Api` actúa como **Composition Root**, donde se conectan los casos de uso con los adaptadores concretos mediante inyección de dependencias.

## Autenticación y autorización

El Api **no implementa login ni gestiona usuarios**: solo valida el JWT
emitido por `MotorScoring.Identity` (servicio externo, puerto `8082`) y
autoriza según los permisos que ese token trae como claims.

```text
Authorization: Bearer <JWT>
 ↓
JwtBearer valida firma, issuer, audience y expiración
 ↓
Policy por endpoint (RequireClaim "permission", "<permiso>")
 ↓
Permitido / 403
```

Un `401 Unauthorized` indica un token ausente, inválido o expirado. Un
`403 Forbidden` indica un token válido sin el permiso requerido para
esa operación.

Cada endpoint funcional exige un permiso específico mediante política
de autorización:

| Endpoint | Policy exigida |
|---|---|
| `POST /api/v1/solicitudes-credito` | `Scoring.Solicitud.Crear` |
| `POST /api/v1/solicitudes-credito/{id}/evaluar` | `Scoring.Evaluacion.Ejecutar` |

La validación del JWT requiere que `Jwt:Issuer`, `Jwt:Audience` y
`Jwt:SigningKey` coincidan exactamente con los configurados en
`MotorScoring.Identity`, ya que ambos servicios firman/validan con la
misma clave simétrica sin comunicarse entre sí en tiempo de ejecución.

```json
{
  "Jwt": {
    "Issuer": "MotorScoring.Identity",
    "Audience": "MotorScoring.Api",
    "SigningKey": "clave-de-al-menos-32-bytes-igual-a-la-de-Identity"
  }
}
```

`GET /health` no requiere autenticación.

------------------------------------------------------------------------

## Endpoints

Los endpoints mantienen versionado mediante `/api/v1`.

### RF04 — Registrar solicitud

```http
POST /api/v1/solicitudes-credito
Content-Type: application/json
```

Ejemplo:

```json
{
  "identificadorExterno": "SOL-EXT-000001",
  "solicitante": {
    "tipoDocumento": "DNI",
    "numeroDocumento": "12345678",
    "nombresRazonSocial": "Juan Perez",
    "ingresosMensuales": 5000.00,
    "gastosMensuales": 1500.00,
    "obligacionesFinancieras": 500.00,
    "antiguedadLaboralNegocio": 36,
    "numeroObligacionesActivas": 1,
    "puntajeHistorialPagos": 85,
    "alertasMora": 0
  },
  "codigoProducto": "PRESTAMO_PERSONAL",
  "montoSolicitado": 15000.00,
  "plazoSolicitado": 24,
  "moneda": "PEN",
  "finalidadCredito": "Capital de trabajo",
  "canalOrigen": "WEB"
}
```

Respuesta esperada:

```http
201 Created
```

La solicitud queda inicialmente en estado:

```text
REGISTRADA
```

Ejemplo de respuesta:

```json
{
  "idSolicitud": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "idSolicitante": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "identificadorExterno": "SOL-EXT-000001",
  "codigoProducto": "PRESTAMO_PERSONAL",
  "montoSolicitado": 15000,
  "plazoSolicitado": 24,
  "moneda": "PEN",
  "estado": "REGISTRADA",
  "fechaRegistro": "2026-07-27T06:43:40.8807096+00:00"
}
```

### RF05 y RF06 — Validar y calcular scoring

```http
POST /api/v1/solicitudes-credito/{idSolicitud}/evaluar
```

El flujo:

1. Obtiene la solicitud registrada.
2. Obtiene la información financiera del solicitante.
3. Valida que la solicitud pueda ser evaluada.
4. Obtiene el producto crediticio.
5. Obtiene el modelo y la versión vigente.
6. Verifica la configuración y los pesos de los factores.
7. Calcula los valores financieros derivados.
8. Evalúa cada factor contra sus reglas.
9. Calcula el puntaje ponderado.
10. Evalúa reglas excluyentes.
11. Determina el resultado final.
12. Persiste la evaluación y el detalle de los factores.

Ejemplo de resultado:

```json
{
  "idEvaluacion": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "idSolicitud": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "puntajeTotal": 1000,
  "resultado": "PREAPROBADA",
  "estado": "COMPLETADA",
  "versionModelo": "1.1.0",
  "fechaEvaluacion": "2026-07-27T06:45:43.7488619+00:00",
  "factores": []
}
```

## Modelo de scoring

Producto inicial:

```text
Producto: PRESTAMO_PERSONAL
Moneda: PEN
Monto: 1 000 a 50 000
Plazo: 6 a 48 meses
Modelo: MODELO_PERSONAL
Versión vigente: 1.1.0
```

La versión `1.1.0` utiliza los siguientes factores:

| Factor | Peso |
|---|---:|
| Historial de pagos | 22.50 % |
| Relación deuda-ingreso | 18.00 % |
| Capacidad de pago | 18.00 % |
| Estabilidad de ingresos | 13.50 % |
| Antigüedad laboral | 9.00 % |
| Obligaciones activas | 4.50 % |
| Monto frente a capacidad | 4.50 % |
| Alertas de mora | 0.00 % |
| Relación cuota-ingreso | 10.00 % |
| **Total** | **100.00 %** |

Los valores y reglas del modelo son ficticios y se utilizan únicamente con fines académicos.

## Escala del scoring

El motor calcula un puntaje entre `0` y `1000`.

```text
0   - 599  → RECHAZADA
600 - 749  → REVISION_MANUAL
750 - 1000 → PREAPROBADA
```

El resultado no depende únicamente del puntaje.

Una **regla excluyente** puede determinar directamente el resultado de la evaluación. Por ejemplo, una alerta de mora activa puede producir:

```text
resultado: RECHAZADA
estado: CON_REGLA_EXCLUYENTE
```

aunque los demás factores produzcan un puntaje favorable.

## Cálculos principales

### Capacidad de pago

```text
Capacidad =
    IngresosMensuales
    - GastosMensuales
    - ObligacionesFinancieras
```

### Relación deuda-ingreso

```text
RDI =
    ObligacionesFinancieras
    / IngresosMensuales
    × 100
```

### Relación cuota-ingreso

La cuota estimada se obtiene mediante:

```text
CuotaEstimada =
    MontoSolicitado
    / PlazoSolicitado
```

y posteriormente:

```text
RCI =
    CuotaEstimada
    / IngresosMensuales
    × 100
```

### Monto frente a capacidad

```text
MontoCapacidad =
    MontoSolicitado
    / (CapacidadDisponible × PlazoSolicitado)
    × 100
```

## SQL Server y DbUp

La aplicación utiliza **SQL Server** como base de datos.

El esquema se administra mediante **DbUp**, evitando crear manualmente las tablas al iniciar un entorno nuevo.

Los scripts se encuentran en el proyecto `MotorScoring.Api` y se ejecutan en orden:

```text
001_CreateSchema.sql
002_InitialScoringModel.sql
003_AddRelacionCuotaIngreso.sql
```

### 001 — CreateSchema

Crea las tablas del modelo.

### 002 — InitialScoringModel

Carga:

- `MODELO_PERSONAL`
- versión `1.0.0`
- producto `PRESTAMO_PERSONAL`
- factores iniciales
- reglas de evaluación

### 003 — AddRelacionCuotaIngreso

Evoluciona el modelo a la versión `1.1.0`, agrega:

```text
RELACION_CUOTA_INGRESO
```

y redistribuye los pesos manteniendo un total de `100 %`.

DbUp registra los scripts ejecutados en:

```text
SchemaVersions
```

## Modelo de datos

Tablas funcionales:

```text
solicitantes
solicitudes_credito
productos_crediticios
modelos_scoring
versiones_modelo
factores_scoring
reglas_evaluacion
evaluaciones_crediticias
resultados_factor
resultados_scoring
```

Tabla técnica:

```text
SchemaVersions
```

Los identificadores principales utilizan `UNIQUEIDENTIFIER` en SQL Server y `Guid` en .NET.

## Configurar la conexión

Configure la cadena de conexión `MotorScoringDb` en `appsettings.json` o mediante una fuente de configuración apropiada para el entorno.

Ejemplo para SQL Server Express:

```json
{
  "ConnectionStrings": {
    "MotorScoringDb": "Data Source=localhost\\SQLEXPRESS,1433;Initial Catalog=MotorScoring;User ID=sa;Password=TU_PASSWORD;Encrypt=False;Trust Server Certificate=True"
  }
}
```

No almacene credenciales reales en el repositorio.

Para ambientes distintos de desarrollo utilice variables de entorno, secretos o el mecanismo de gestión de secretos correspondiente.

## Compilar

Requisito: **.NET 8 SDK**.

Desde la raíz de la solución:

```bash
dotnet restore
dotnet build
```

Para ejecutar las pruebas:

```bash
dotnet test
```
 Para mas detalle
```bash
dotnet test --logger "console;verbosity=detailed"
```

Una compilación correcta debe finalizar sin errores.

## Levantar localmente

Ejecute:

```bash
dotnet run --project ./src/MotorScoring.Api/MotorScoring.Api.csproj
```

En Windows PowerShell:

```powershell
dotnet run --project .\src\MotorScoring.Api\MotorScoring.Api.csproj
```

Al iniciar por primera vez, DbUp crea la base y ejecuta los scripts pendientes.

La aplicación mostrará en consola las URLs asignadas por ASP.NET Core, por ejemplo:

```text
http://localhost:56700
https://localhost:56699
```

Los puertos pueden variar según la configuración de `launchSettings.json`.

## Swagger

Con el entorno `Development`, Swagger queda disponible en:

```text
http://localhost:<puerto>/swagger
```

Ejemplo local:

```text
http://localhost:56700/swagger
```

Desde Swagger puede ejecutarse el flujo:

```text
1. POST /api/v1/solicitudes-credito
                 │
                 ▼
        Solicitud REGISTRADA
                 │
                 ▼
2. Obtener idSolicitud
                 │
                 ▼
3. POST /api/v1/solicitudes-credito/{idSolicitud}/evaluar
                 │
                 ▼
        Evaluación COMPLETADA
```

## Health Check

La API expone:

```http
GET /health
```

Este endpoint permite verificar el estado de la aplicación y la disponibilidad de SQL Server configurada por el proyecto.

## Pruebas realizadas

Durante la validación funcional se comprobaron al menos dos escenarios principales.

### Caso favorable

Datos con:

```text
Historial de pagos alto
Endeudamiento bajo
Capacidad de pago alta
Antigüedad alta
Sin alertas de mora
Relación cuota-ingreso baja
```

Resultado:

```text
Puntaje: 1000
Resultado: PREAPROBADA
Estado: COMPLETADA
```

### Regla excluyente

Se utiliza una solicitud con condiciones financieras favorables pero:

```text
AlertasMora = 1
```

El factor `ALERTAS_MORA` activa una regla excluyente y fuerza:

```text
Resultado: RECHAZADA
Estado: CON_REGLA_EXCLUYENTE
```

Esto permite comprobar que las reglas excluyentes tienen prioridad sobre la clasificación obtenida únicamente por puntaje.

## Flujo Hexagonal

Para registrar una solicitud:

```text
HTTP Request
    │
    ▼
SolicitudesCreditoController
    │
    │ Adaptador de entrada
    ▼
Puerto / Caso de uso
    │
    ▼
RegistrarSolicitudUseCase
    │
    ▼
Domain
    │
    ▼
Puerto de persistencia
    ▲
    │
Repositorio EF Core
    ▲
    │ Adaptador de salida
    ▼
SQL Server
```

Para evaluar:

```text
HTTP
 │
 ▼
Controller
 │
 ▼
EjecutarEvaluacionScoringUseCase
 │
 ▼
CalculadorScoring
 │
 ├── Capacidad de pago
 ├── Relación deuda-ingreso
 ├── Relación cuota-ingreso
 ├── Reglas por factor
 ├── Puntaje ponderado
 └── Reglas excluyentes
 │
 ▼
Puertos de salida
 │
 ▼
EF Core
 │
 ▼
SQL Server
```

## Docker

El proyecto incluye `Dockerfile` para contenerizar la API.

Antes de construir la imagen es recomendable validar localmente:

```bash
dotnet restore
dotnet build
dotnet test
```

Luego puede construirse la imagen según la configuración definida en el repositorio.

La conexión a SQL Server debe configurarse de acuerdo con el entorno donde se ejecute el contenedor.

## Principios aplicados

La implementación busca mantener:

- Independencia del dominio respecto de frameworks.
- Separación entre reglas de negocio y mecanismos externos.
- Puertos para definir las operaciones requeridas por la aplicación.
- Adaptadores de entrada para REST.
- Adaptadores de salida para persistencia.
- Inversión de dependencias.
- Inyección de dependencias desde el Composition Root.
- Value Objects para representar conceptos del dominio.
- Reglas del scoring dentro del núcleo y no en controllers o SQL.
- Versionamiento del modelo de scoring.
- Persistencia desacoplada del dominio.
