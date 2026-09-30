# MotorScoring.Identity — Arquitectura Hexagonal

Servicio de identidad centralizado del Motor de Scoring Crediticio,
implementado con **ASP.NET Core Identity** sobre **Arquitectura
Hexagonal**, sin cambiar los contratos HTTP, rutas ni el puerto `8082`
usado por `docker-compose`.

Resuelve autenticación local, Google, GitHub y MFA/TOTP para
`MotorScoring.Web`, y emite los JWT que `MotorScoring.Api` valida.

------------------------------------------------------------------------

## Módulos

- `MotorScoring.Identity.Domain`: entidades (`UserAccount`, `UserSummary`) y `SecurityConstants` (nombres de claims y permisos).
- `MotorScoring.Identity.Application`: casos de uso (`AuthUseCase`, `UserAdministrationUseCase`), contratos HTTP y puertos de entrada/salida.
- `MotorScoring.Identity.Adapters.Inbound.Api`: `AuthController` y `UsersController`, con las mismas rutas y contratos existentes.
- `MotorScoring.Identity.Adapters.Outbound.Identity`: ASP.NET Core Identity, EF Core, SQL Server, JWT, refresh tokens y proveedores Google/GitHub.
- `MotorScoring.Identity.Api`: host / composition root (`Program.cs`, seeder, Swagger, health check).

## Compatibilidad

Se conservan las rutas existentes bajo `/api/auth` y `/api/users`, los
modelos JSON de entrada/salida, el puerto `8082`, el nombre final
`MotorScoring.Identity.dll`, el `Dockerfile` en la raíz de
`MotorScoring.Identity` y el contexto de build usado por Docker Compose.

------------------------------------------------------------------------

## Endpoints — `/api/auth`

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| POST | `/register` | Anónimo | Registro local. Solo funciona si `Security:AllowSelfRegistration = true`; si no, devuelve `403`. |
| POST | `/login` | Anónimo | Login local con email + password. |
| POST | `/mfa/verify` | Anónimo | Completa el login con un código TOTP, usando el `challengeToken` recibido en `/login`. |
| POST | `/mfa/recovery` | Anónimo | Completa el login con un recovery code, en lugar del código TOTP. |
| POST | `/refresh` | Anónimo | Cambia un refresh token vigente por un nuevo par access/refresh. |
| POST | `/revoke` | Anónimo | Revoca un refresh token (usado en logout). |
| GET | `/me` | JWT | Devuelve email, roles y permisos del usuario autenticado. |
| POST | `/mfa/setup` | JWT | Genera (o reutiliza) la clave TOTP y el `AuthenticatorUri`. |
| POST | `/mfa/enable` | JWT | Valida el primer código TOTP y activa MFA; devuelve los recovery codes. |
| POST | `/mfa/disable` | JWT | Desactiva MFA y resetea la clave del autenticador. |
| GET | `/external/{provider}` | Anónimo | Inicia el `Challenge` OAuth (`Google` o `GitHub`) mediante el esquema externo de Identity. |
| GET | `/external/callback` | Anónimo | Callback OAuth: crea o vincula el `AspNetUser` y redirige al Web con un código de intercambio. |
| POST | `/external/exchange` | Anónimo | Cambia el código de intercambio recibido por el Web por un login (o un desafío MFA). |

## Endpoints — `/api/users`

Todos requieren rol `Administrador`.

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/` | Lista usuarios con `Id`, `Email`, `DisplayName`, `TwoFactorEnabled`, `LockoutEnd`. |
| PUT | `/{id}/roles/{roleName}` | Asigna un rol al usuario. |
| DELETE | `/{id}/roles/{roleName}` | Quita un rol al usuario. |

Esto permite asignar el rol `Analista` desde la propia API en lugar de
hacerlo únicamente por SQL directo contra `AspNetUserRoles`.

------------------------------------------------------------------------

## Login local

```text
POST /api/auth/login
 ↓
Buscar usuario por email
 ↓
¿Cuenta bloqueada (Lockout)? → 401
 ↓
Validar password hash
 │  falla → AccessFailedAsync (cuenta hacia lockout) → 401
 ↓
ResetAccessFailedCount
 ↓
¿MfaEnabled? ──sí──▶ LoginResponse(RequiresMfa=true, MfaChallengeToken)
     │no
     ▼
LoginResponse(Succeeded=true, Tokens = access + refresh)
```

Política de contraseña (`AddIdentityCore`): mínimo 10 caracteres,
requiere dígito, mayúscula, minúscula y símbolo. Lockout: 5 intentos
fallidos → bloqueo de 15 minutos.

------------------------------------------------------------------------

## Login con Google / GitHub

```text
GET /api/auth/external/{provider}
 ↓
Challenge OAuth (esquema "ExternalScheme", cookie temporal de 10 min)
 ↓
Proveedor autentica al usuario
 ↓
GET /api/auth/external/callback
 ↓
Busca AspNetUser por (provider, providerKey)
   no existe ──▶ busca por email ──▶ si tampoco existe, crea AspNetUser nuevo
 ↓
Vincula el login externo (AspNetUserLogins) si no estaba vinculado
 ↓
Genera un código de intercambio de un solo uso (JWT corto, 2 minutos, purpose=external_exchange)
 ↓
Redirige a Web: {WebClient:BaseUrl}/Account/ExternalCallback?code=...
 ↓
Web llama POST /api/auth/external/exchange
 ↓
¿MfaEnabled? ──sí──▶ RequiresMfa + MfaChallengeToken
     │no
     ▼
Tokens (access + refresh)
```

**GitHub es OAuth2 puro, no OIDC**: se registra con `AddOAuth`, no
`AddGitHub`. Tras el intercambio del `AccessToken`, el adaptador hace
una llamada adicional a `GET https://api.github.com/user` para obtener
`id`, `name` y `login`. Si GitHub no devuelve el email en ese response
(email privado), se hace una segunda llamada a
`GET https://api.github.com/user/emails` y se toma el primer email
marcado como `primary` y `verified`.

Si el `providerKey` de un proveedor nuevo corresponde a un email ya
existente en otro `AspNetUser` (por ejemplo, ya logueado antes con
Google), el nuevo login (GitHub) se vincula a **ese mismo usuario**, en
lugar de crear uno nuevo. Esto es lo que permite que Google y GitHub
compartan roles, permisos y estado de MFA cuando corresponden a la
misma persona.

Si un usuario externo es completamente nuevo (proveedor + email sin
match previo), se le crea un `AspNetUser` con
`{provider}-{providerKey}@external.motorscoring.local` como email solo
si el proveedor no entregó ninguno.

------------------------------------------------------------------------

## MFA / TOTP

MFA es opcional, controlado por usuario (`TwoFactorEnabled`), no
global.

### Configuración

```text
POST /api/auth/mfa/setup   (requiere JWT)
 ↓
Reutiliza la clave TOTP existente o genera una nueva (ResetAuthenticatorKey)
 ↓
Devuelve { sharedKey, authenticatorUri }
   authenticatorUri = otpauth://totp/{issuer}:{email}?secret={key}&issuer={issuer}&digits=6
```

El `issuer` es configurable vía `Mfa:Issuer` (por defecto
`MotorScoring`).

### Activación

```text
POST /api/auth/mfa/enable  { code }  (requiere JWT)
 ↓
Verifica el código TOTP contra la clave generada en /mfa/setup
   inválido → 400
 ↓
TwoFactorEnabled = true
 ↓
Genera 8 recovery codes de un solo uso
```

### Desactivación

```text
POST /api/auth/mfa/disable  (requiere JWT)
 ↓
TwoFactorEnabled = false
Se resetea la clave del autenticador (una futura reactivación exige un QR nuevo)
```

### Desafío en login

Al hacer login (local o externo) con `MfaEnabled = true`, Identity no
entrega tokens todavía: entrega un `MfaChallengeToken`, que es un JWT
de **5 minutos de vida** con `purpose=mfa`. Ese token se envía de
vuelta en `/api/auth/mfa/verify` (código TOTP) o `/api/auth/mfa/recovery`
(recovery code) para completar el login y recién ahí recibir el
access/refresh token.

------------------------------------------------------------------------

## Roles y permisos

Roles sembrados por `IdentitySeeder`:

```text
Administrador
Analista
```

Permisos (almacenados como `RoleClaims` de tipo `permission`):

```text
Scoring.Solicitud.Crear
Scoring.Evaluacion.Ejecutar
```

Actualmente **ambos roles reciben ambos permisos** durante el seed —
la distinción entre `Administrador` y `Analista` hoy solo se refleja en
que `/api/users` exige explícitamente el rol `Administrador`, no en los
permisos funcionales del Api de Scoring.

El usuario administrador inicial se crea con:

```yaml
Seed:AdminEmail: "admin@motorscoring.local"
Seed:AdminPassword: "Admin1234*"
```

y queda asignado automáticamente al rol `Administrador`.

------------------------------------------------------------------------

## JWT y Refresh Tokens

El access token es un JWT HMAC-SHA256 que incluye:

```text
sub, email, jti
ClaimTypes.NameIdentifier, ClaimTypes.Name
mfa (true/false)
ClaimTypes.Role     (uno por rol)
permission          (uno por permiso, de rol y/o de usuario)
```

`Jwt:SigningKey` debe tener al menos 32 bytes; si no, el servicio falla
al iniciar. La duración del access token y del refresh token se
configuran con `Jwt:AccessTokenMinutes` (60 min por defecto) y
`Jwt:RefreshTokenDays` (7 días por defecto).

`Jwt:Issuer`, `Jwt:Audience` y `Jwt:SigningKey` deben ser **idénticos**
a los configurados en `MotorScoring.Api` (`MotorScoring.Hexagonal.Net8`),
ya que ese servicio valida el JWT emitido aquí sin comunicarse
directamente con Identity. En `docker-compose.yml` ambos servicios
reciben los mismos tres valores.

El refresh token es un valor aleatorio de 64 bytes (no un JWT); se
persiste **hasheado con SHA-256** en la tabla `RefreshTokens`, junto
con su expiración. Al usar `/api/auth/refresh`:

```text
Busca el hash del refresh token recibido
 ↓
¿No existe, revocado o expirado? → 401
 ↓
Marca el token actual como revocado (RevokedAt)
 ↓
Emite un nuevo par access/refresh
 ↓
Guarda en el token viejo el hash del nuevo (ReplacedByTokenHash) — rotación encadenada
```

`/api/auth/revoke` simplemente marca `RevokedAt` sobre el refresh token
recibido, sin emitir uno nuevo (usado en logout).

Los códigos de desafío MFA (`MfaChallengeToken`) y de intercambio
externo (`ExternalExchangeCode`) también son JWT firmados con la misma
clave, pero de muy corta vida (5 y 2 minutos respectivamente) y con un
claim `purpose` que impide reutilizarlos como access token.

------------------------------------------------------------------------

## Base de datos

`MotorScoringIdentity` (SQL Server vía EF Core, `EnsureCreatedAsync` en
el seeder) contiene, entre otras, las tablas estándar de ASP.NET Core
Identity:

```text
AspNetUsers
AspNetRoles
AspNetUserRoles
AspNetUserClaims
AspNetRoleClaims
AspNetUserLogins
AspNetUserTokens
RefreshTokens
```

------------------------------------------------------------------------

## Configuración

```yaml
ConnectionStrings__IdentityDb: "..."

Jwt__Issuer: "MotorScoring.Identity"
Jwt__Audience: "MotorScoring.Api"
Jwt__SigningKey: "clave-de-al-menos-32-bytes"
Jwt__AccessTokenMinutes: 60   # valor por defecto si se omite
Jwt__RefreshTokenDays: 7      # valor por defecto si se omite

Mfa__Issuer: "MotorScoring"

Security__AllowSelfRegistration: false

WebClient__BaseUrl: "http://localhost:8081"

Authentication__Google__ClientId: "CLIENT_ID"
Authentication__Google__ClientSecret: "CLIENT_SECRET"

Authentication__GitHub__ClientId: "CLIENT_ID"
Authentication__GitHub__ClientSecret: "CLIENT_SECRET"

Seed__AdminEmail: "admin@motorscoring.local"
Seed__AdminPassword: "Admin1234*"
```

Callbacks usados en desarrollo:

```text
http://localhost:8082/signin-google
http://localhost:8082/signin-github
```

Un proveedor externo solo queda habilitado si su `ClientId` está
configurado; si no, `/api/auth/external/{provider}` responde `400`.

Las credenciales reales no deben almacenarse en el repositorio.

------------------------------------------------------------------------

## Swagger y Health Check

En entorno `Development`:

```text
http://localhost:8082/swagger
```

Health check:

```http
GET /health
```
