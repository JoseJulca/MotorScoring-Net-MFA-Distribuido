# MotorScoring.Web

Frontend desarrollado con **.NET 8 + ASP.NET Core MVC + Razor** para el
Motor de Scoring Crediticio.

Consume dos servicios independientes:

- **MotorScoring.Identity** (`:8082`): autenticación local, Google, GitHub y MFA.
- **MotorScoring.Api** (`:8080`): implementación en **.NET 8 + Arquitectura Hexagonal**
  del Motor de Scoring.

El Web **no valida contraseñas, roles ni permisos**. Esa responsabilidad
pertenece a `MotorScoring.Identity` y a `MotorScoring.Api`
respectivamente.

------------------------------------------------------------------------

## Responsabilidades

-   Pantalla de Login.
-   Inicio de sesión local (correo + contraseña).
-   Inicio de sesión mediante Google y GitHub.
-   Pantalla de desafío MFA (código TOTP) y de código de recuperación.
-   Configuración de MFA (QR + clave secreta manual).
-   Conservación de la sesión Web mediante cookie de autenticación.
-   Almacenamiento del access token y refresh token emitidos por Identity.
-   Consumo de `MotorScoring.Api` utilizando el JWT como `Bearer`.

------------------------------------------------------------------------

## Compatibilidad con el backend

Este frontend consume las rutas del backend **.NET 8 Hexagonal**
(`MotorScoring.Hexagonal.Net8`), versionadas bajo `/api/v1`:

```http
POST /api/v1/solicitudes-credito
POST /api/v1/solicitudes-credito/{idSolicitud}/evaluar
```

Los identificadores (`idSolicitud`, `idSolicitante`, `idEvaluacion`) se
modelan como `Guid`.

------------------------------------------------------------------------

## Autenticación

### Login local

```text
Web (POST /Account/Login)
 ↓
Identity: POST /api/auth/login
 ↓
¿RequiresMfa?
 ├── Sí → Web: /Account/Mfa
 └── No → SignIn (cookie) → Crear solicitud
```

### Login con Google / GitHub

```text
Web: GET /Account/ExternalLogin?provider=Google|GitHub
 ↓
Redirect a Identity: /api/auth/external/{provider}
 ↓
Identity gestiona el flujo OAuth con el proveedor
 ↓
Callback: Web /Account/ExternalCallback?code=...
 ↓
Identity: POST /api/auth/external/exchange
 ↓
¿RequiresMfa? → igual que el login local
```

### Desafío MFA

Si `RequiresMfa = true`, Identity devuelve un `MfaChallengeToken` y el
Web redirige a una de estas dos pantallas:

```http
GET/POST /Account/Mfa            → Identity: POST /api/auth/mfa/verify
GET/POST /Account/RecoveryCode   → Identity: POST /api/auth/mfa/recovery
```

Ambas, si tienen éxito, completan el login (`SignIn` + cookie) igual
que el login local o externo.

### Cierre de sesión

```text
POST /Account/Logout
 ↓
Identity: POST /api/auth/revoke (revoca el refresh token)
 ↓
SignOut de la cookie del Web
```

------------------------------------------------------------------------

## Configuración de MFA (QR)

```http
GET  /Account/MfaSetup   → Identity: POST /api/auth/mfa/setup
POST /Account/MfaSetup   → Identity: POST /api/auth/mfa/enable
```

El `AuthenticatorUri` devuelto por Identity se convierte en un **QR
generado en el servidor** (`QrCodeService`, basado en QRCoder). La URI
`otpauth://` no se expone en la interfaz; la clave secreta (`SharedKey`)
permanece oculta y solo se muestra como alternativa de configuración
manual.

Al habilitar MFA correctamente, la vista muestra los **recovery codes**
generados por Identity.

------------------------------------------------------------------------

## Sesión y tokens

-   La sesión Web se mantiene mediante **cookie authentication**
    (`MotorScoring.Web.Auth`), no mediante sesión de servidor con estado.
-   El access token y el refresh token emitidos por Identity se
    conservan como parte de los tokens de la cookie de autenticación
    (`AuthenticationProperties`), a través de `WebSignInService` /
    `TokenSessionService`.
-   `TokenSessionService` expone un access token válido para cada
    llamada a `MotorScoring.Api`; si está vencido, gestiona su renovación
    contra `Identity: POST /api/auth/refresh`.
-   Un `401` de `MotorScoring.Api` indica un problema de token; un
    `403` indica que el usuario está autenticado pero no tiene el
    permiso requerido.

------------------------------------------------------------------------

## Flujo de dos pasos (registro y evaluación)

1.  El usuario completa el formulario de solicitud.
2.  `Registrar solicitud` consume `POST /api/v1/solicitudes-credito`
    (con `Authorization: Bearer <JWT>`).
3.  Si responde `201 Created`, se conserva el `idSolicitud` (`Guid`) y
    se mantiene visible el `identificadorExterno` ingresado.
4.  Los campos quedan bloqueados y se habilita `Evaluar solicitud`.
5.  `Evaluar solicitud` consume
    `POST /api/v1/solicitudes-credito/{idSolicitud}/evaluar`.
6.  Si la evaluación falla, no se vuelve a registrar la solicitud.

------------------------------------------------------------------------

## Pantallas

-   Login (`Account/Login`).
-   Desafío MFA (`Account/Mfa`).
-   Código de recuperación (`Account/RecoveryCode`).
-   Configuración de MFA (`Account/MfaSetup`).
-   Acceso denegado (`Account/AccessDenied`).
-   Registrar solicitud de crédito (`SolicitudesCredito/Crear`).
-   Resultado de evaluación (`SolicitudesCredito/Resultado`).

La interfaz no muestra los identificadores técnicos innecesarios ni la
URI TOTP cruda.

------------------------------------------------------------------------

## Configuración

Editar `appsettings.json`:

```json
{
  "MotorScoringApi": {
    "BaseUrl": "http://localhost:8080"
  },
  "Identity": {
    "BaseUrl": "http://localhost:8082",
    "PublicBaseUrl": "http://localhost:8082"
  }
}
```

-   `MotorScoringApi:BaseUrl`: URL interna usada por el `HttpClient`
    hacia `MotorScoring.Api`.
-   `Identity:BaseUrl`: URL interna usada por el `HttpClient` hacia
    `MotorScoring.Identity`.
-   `Identity:PublicBaseUrl`: URL pública a la que el navegador es
    redirigido para iniciar el login externo
    (`/api/auth/external/{provider}`). Puede diferir de `BaseUrl` en
    entornos donde el Web accede a Identity por una red interna distinta
    de la que usa el navegador del usuario.

------------------------------------------------------------------------

## Ejecutar

```powershell
dotnet restore
dotnet build
dotnet run
```

Requiere que `MotorScoring.Identity` y `MotorScoring.Api` estén
disponibles en las URLs configuradas (por defecto, vía Docker Compose
en `:8082` y `:8080`).

------------------------------------------------------------------------

## Diferencias frente a versiones anteriores

Este frontend reemplazó una versión previa que consumía un backend
**Java 21 + Spring Boot + Onion** con rutas sin versionar
(`/api/solicitudes-credito`, identificadores `long`, fechas
`LocalDateTime`). La versión actual consume exclusivamente
`MotorScoring.Hexagonal.Net8`:

| Aspecto | Versión actual (.NET Hexagonal) |
|---|---|
| Registro | `/api/v1/solicitudes-credito` |
| Evaluación | `/api/v1/solicitudes-credito/{id}/evaluar` |
| Identificadores | `Guid` |
| Autenticación | Vía `MotorScoring.Identity` (JWT + MFA), no embebida en el Web |
