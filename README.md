# Motor Scoring Crediticio --- Arquitectura Distribuida

## 1. Objetivo

Este repositorio implementa una solución de **Motor de Scoring
Crediticio** evolucionada hacia una arquitectura distribuida. La
solución separa responsabilidades de negocio, identidad, notificaciones,
procesamiento asíncrono, mensajería, gateway y observabilidad en
componentes independientes que pueden desplegarse y ejecutarse mediante
Docker Compose.

La evolución realizada tuvo cuatro objetivos principales:

1.  Separar responsabilidades en servicios independientes.
2.  Incorporar comunicación síncrona y asíncrona.
3.  Centralizar el acceso HTTP mediante un API Gateway.
4.  Incorporar observabilidad de infraestructura sin modificar el código
    funcional de los servicios.

------------------------------------------------------------------------

## 2. Arquitectura final

``` text
                           ┌─────────────────────┐
                           │     Usuario/Web     │
                           └──────────┬──────────┘
                                      │
                                      ▼
                           ┌─────────────────────┐
                           │    Kong Gateway     │
                           │       :8000         │
                           └──────┬────────┬─────┘
                                  │        │
                 /api/auth/*      │        │ /api/v1/*
                                  │        │
                                  ▼        ▼
                    ┌────────────────┐   ┌────────────────────┐
                    │ Identity.Api   │   │ MotorScoring.Api   │
                    │     :8082      │   │       :8080        │
                    └───────┬────────┘   └─────────┬──────────┘
                            │                      │
                            ▼                      │ HTTP
                    ┌───────────────┐              ▼
                    │  SQL Server   │    ┌─────────────────────────┐
                    │ Identity DB   │    │ Notificaciones.Api      │
                    └───────────────┘    │         :8083           │
                                         └──────┬──────────┬───────┘
                                                │          │
                                         RabbitMQ          Kafka
                                                │          │
                                                ▼          ▼
                                   ┌──────────────────┐ ┌─────────────────┐
                                   │ Notification     │ │ Audit.Worker    │
                                   │ Worker           │ │                 │
                                   └──────────────────┘ └────────┬────────┘
                                                                 │
                                                                 ▼
                                                      auditoria-scoring.jsonl


                ─────────── OBSERVABILIDAD DE INFRAESTRUCTURA ───────────

                    Contenedores Docker
                            │
                            ▼
                    ┌─────────────────┐
                    │    cAdvisor     │
                    │      :8084      │
                    └────────┬────────┘
                             │ métricas
                             ▼
                    ┌─────────────────┐
                    │   Prometheus    │
                    │      :9090      │
                    └────────┬────────┘
                             │
                             ▼
                    ┌─────────────────┐
                    │     Grafana     │
                    │      :3000      │
                    └─────────────────┘
```

------------------------------------------------------------------------

## 3. Componentes principales

### MotorScoring.Web

Aplicación web de la solución. Consume los servicios HTTP a través de
Kong Gateway. El navegador continúa accediendo al Web por su puerto
publicado, mientras que las llamadas hacia Identity y MotorScoring se
centralizan mediante el Gateway.

### MotorScoring.Api

API principal del motor de scoring. Implementa la lógica relacionada con
solicitudes y evaluación crediticia utilizando arquitectura hexagonal.

Después de una evaluación puede realizar una llamada HTTP a
`Notificaciones.Api`. Esta integración es síncrona a nivel HTTP y
actualmente es **best-effort**; no se implementó Outbox transaccional
para esta comunicación.

### MotorScoring.Identity

Servicio independiente responsable de autenticación y autorización.

Incluye:

-   ASP.NET Core Identity.
-   Login local.
-   Google OAuth.
-   GitHub OAuth.
-   JWT y refresh tokens.
-   MFA mediante TOTP.
-   Códigos de recuperación.
-   Roles y permisos.
-   Rol `Analista` asignado automáticamente a nuevos usuarios externos
    de Google/GitHub.
-   Rol `Administrador` disponible para administración.

Identity mantiene su propia persistencia lógica separada de la
información de scoring.

### MotorScoring.Notificaciones.Api

API independiente organizada con arquitectura hexagonal.

Su responsabilidad es recibir solicitudes de notificación y publicar
eventos hacia los mecanismos de mensajería correspondientes.

Publica:

-   eventos de notificación en RabbitMQ;
-   eventos de auditoría en Kafka.

El proyecto mantiene separación entre Application, Infrastructure y API.
El Domain permanece vacío cuando no existe lógica de dominio que
justifique agregar elementos artificialmente.

------------------------------------------------------------------------

## 4. Separación de los Worker Services

Uno de los cambios realizados fue separar los consumidores en proyectos
independientes, en lugar de mantenerlos anidados dentro del proyecto de
Notificaciones.

La estructura conceptual queda:

``` text
MotorScoring.Notificaciones
MotorScoring.Notification.Worker
MotorScoring.Audit.Worker
```

Cada uno puede compilarse y ejecutarse independientemente.

### Notification.Worker

Worker Service .NET 8 que consume RabbitMQ.

Cola utilizada:

``` text
scoring.notificaciones.email
```

El Worker recibe el mensaje publicado por `Notificaciones.Api` y simula
el procesamiento/envío de la notificación.

Flujo:

``` text
Notificaciones.Api
        │
        ▼
    RabbitMQ
        │
        ▼
Notification.Worker
```

### Audit.Worker

Worker Service .NET 8 que consume eventos desde Kafka.

Topic:

``` text
scoring.auditoria
```

Los eventos consumidos se almacenan en:

``` text
/app/audit/auditoria-scoring.jsonl
```

Flujo:

``` text
Notificaciones.Api
        │
        ▼
       Kafka
        │
        ▼
  Audit.Worker
        │
        ▼
auditoria-scoring.jsonl
```

### Contratos de mensajería

Se eliminó el proyecto compartido `MotorScoring.Messaging.Contracts`.

Cada consumidor mantiene el modelo JSON necesario para interpretar los
mensajes que consume. De esta forma, los Workers permanecen como
proyectos independientes y no requieren un ensamblado compartido para
ejecutarse.

------------------------------------------------------------------------

## 5. Comunicación síncrona y asíncrona

La solución demuestra ambos mecanismos.

### Comunicación síncrona

Se utiliza HTTP cuando un componente necesita invocar directamente a
otro servicio.

Ejemplo:

``` text
MotorScoring.Api
       │
       │ HTTP
       ▼
Notificaciones.Api
```

También las llamadas externas de Web hacia los APIs pasan por Kong.

### Comunicación asíncrona

RabbitMQ y Kafka desacoplan productores y consumidores.

``` text
                    ┌── RabbitMQ ──► Notification.Worker
Notificaciones.Api ─┤
                    └── Kafka ─────► Audit.Worker
```

El productor no necesita ejecutar directamente la lógica del Worker.
Publica el mensaje/evento y el consumidor lo procesa de forma
independiente.

------------------------------------------------------------------------

## 6. RabbitMQ

RabbitMQ se utiliza como broker para las notificaciones.

La cola principal implementada es:

``` text
scoring.notificaciones.email
```

La comunicación demuestra desacoplamiento temporal: `Notificaciones.Api`
publica el mensaje y `Notification.Worker` lo procesa como consumidor
independiente.

La interfaz de administración de RabbitMQ está expuesta en el entorno
local para inspeccionar colas, conexiones y consumidores.

------------------------------------------------------------------------

## 7. Kafka

Kafka se utiliza para auditoría basada en eventos.

Topic utilizado:

``` text
scoring.auditoria
```

`Notificaciones.Api` actúa como productor y `Audit.Worker` como
consumidor.

Esto permite demostrar un flujo event-driven diferente al utilizado para
las notificaciones con RabbitMQ.

------------------------------------------------------------------------

## 8. API Gateway con Kong

Se incorporó **Kong Gateway** como punto central de entrada para las
APIs HTTP.

Kong se ejecuta como un contenedor independiente y utiliza configuración
declarativa, por lo que no fue necesario crear un nuevo proyecto .NET.

Puerto:

``` text
http://localhost:8000
```

Rutas principales:

``` text
/api/auth/*       -> MotorScoring.Identity
/api/v1/*         -> MotorScoring.Api
/signin-google    -> MotorScoring.Identity
/signin-github    -> MotorScoring.Identity
```

El Gateway centraliza el enrutamiento HTTP externo.

No todo el tráfico distribuido pasa por Kong. La comunicación interna
`MotorScoring.Api -> Notificaciones.Api` permanece directa y
RabbitMQ/Kafka tampoco pasan por el Gateway.

### OAuth mediante Gateway

Los callbacks externos fueron adecuados para utilizar Kong:

``` text
http://localhost:8000/signin-google
http://localhost:8000/signin-github
```

Se validó el funcionamiento de:

-   login local;
-   MFA;
-   Google;
-   GitHub;
-   JWT;
-   roles y permisos.

------------------------------------------------------------------------

## 9. Usuarios externos y rol Analista

Inicialmente, los usuarios creados mediante Google/GitHub no recibían un
rol y podían autenticarse, pero posteriormente encontraban errores de
autorización.

La creación de usuarios externos fue ajustada para asignar
automáticamente:

``` text
Analista
```

La asignación ocurre únicamente durante la creación de un nuevo usuario
externo.

Por lo tanto, un usuario existente que posteriormente tenga el rol
`Administrador` no es sobrescrito al volver a autenticarse con Google o
GitHub.

El JWT continúa obteniendo los roles del usuario y agregándolos como
claims.

------------------------------------------------------------------------

## 10. Docker Compose y evolución de versiones

La infraestructura se fue ampliando manteniendo versiones del Compose.

``` text
docker-compose.yml
    versión inicial

docker-compose.v2.yml
    + Kong Gateway

docker-compose.v3.yml
    + Kong Gateway
    + cAdvisor
    + Prometheus
    + Grafana
```

La V3 representa el entorno distribuido completo utilizado para las
pruebas actuales.

Inicio:

``` powershell
docker compose -f docker-compose.v3.yml up -d --build
```

Para detener sin eliminar datos:

``` powershell
docker compose -f docker-compose.v3.yml down
```

No utilizar `-v` si se desea conservar los volúmenes.

------------------------------------------------------------------------

## 11. Observabilidad

La observabilidad agregada en V3 es de **infraestructura**. No se
modificó el código de MotorScoring para implementarla.

Arquitectura:

``` text
Docker
   │
   ▼
cAdvisor
   │
   ▼
Prometheus
   │
   ▼
Grafana
```

### cAdvisor

cAdvisor obtiene métricas de los contenedores Docker.

Puerto:

``` text
http://localhost:8084
```

Durante la implementación se detectó que `v0.49.1` solamente exponía
correctamente cgroups generales en el entorno Docker Desktop/WSL2 y no
identificaba los contenedores individuales.

La configuración final utiliza:

``` text
ghcr.io/google/cadvisor:0.54.0
```

con acceso al Docker socket y los mounts necesarios.

Con esta versión se verificó que Prometheus recibe labels como:

``` text
name="motor-scoring-api"
name="motor-scoring-identity"
name="motor-scoring-gateway"
name="motor-scoring-kafka"
name="motor-scoring-rabbitmq"
...
```

### Prometheus

Prometheus almacena las métricas recopiladas por cAdvisor.

Puerto:

``` text
http://localhost:9090
```

Ejemplos de métricas utilizadas:

``` promql
container_memory_usage_bytes
```

``` promql
rate(container_cpu_usage_seconds_total[1m])
```

``` promql
rate(container_network_receive_bytes_total[1m])
```

``` promql
rate(container_network_transmit_bytes_total[1m])
```

### Grafana

Grafana presenta las métricas de Prometheus.

Puerto:

``` text
http://localhost:3000
```

Dashboard:

``` text
Motor Scoring - Infraestructura Docker
```

------------------------------------------------------------------------

## 12. Dashboard de infraestructura

El dashboard final permite observar:

``` text
┌───────────────────────────────────────────────────────────────────────┐
│                 MOTOR SCORING - INFRASTRUCTURE                       │
├─────────────────┬─────────────────┬─────────────────┬─────────────────┤
│ Contenedores UP │ Contenedores    │ CPU Total       │ RAM Total       │
│                 │ DOWN            │                 │                 │
├─────────────────┴─────────────────┴─────────────────┴─────────────────┤
│ CPU POR CONTENEDOR                                                    │
├───────────────────────────────────┬───────────────────────────────────┤
│ MEMORIA POR CONTENEDOR            │ RED / NETWORK                    │
├───────────────────────────────────┴───────────────────────────────────┤
│ ESTADO DE CONTENEDORES                                                │
└───────────────────────────────────────────────────────────────────────┘
```

Se excluyen del conteo funcional los propios componentes de monitoreo:

``` text
motor-scoring-cadvisor
motor-scoring-prometheus
motor-scoring-grafana
```

Los 10 contenedores esperados del sistema son:

``` text
motor-scoring-api
motor-scoring-audit-worker
motor-scoring-database
motor-scoring-gateway
motor-scoring-identity
motor-scoring-kafka
motor-scoring-notificaciones-api
motor-scoring-notification-worker
motor-scoring-rabbitmq
motor-scoring-web
```

------------------------------------------------------------------------

## 13. Detección UP/DOWN

El dashboard no muestra un valor decorativo: se validó la detección real
de caída y recuperación.

Consulta de contenedores UP:

``` promql
sum(
  container_last_seen{
    name=~"motor-scoring-(api|audit-worker|database|gateway|identity|kafka|notificaciones-api|notification-worker|rabbitmq|web)"
  } > bool time() - 30
)
```

Consulta de contenedores DOWN:

``` promql
10 - sum(
  container_last_seen{
    name=~"motor-scoring-(api|audit-worker|database|gateway|identity|kafka|notificaciones-api|notification-worker|rabbitmq|web)"
  } > bool time() - 30
)
```

### Prueba realizada

Se detuvo Identity:

``` powershell
docker stop motor-scoring-identity
```

Resultado observado:

``` text
Contenedores UP      9
Contenedores DOWN    1
```

Posteriormente:

``` powershell
docker start motor-scoring-identity
```

Resultado:

``` text
Contenedores UP      10
Contenedores DOWN     0
```

La recuperación fue reflejada automáticamente por cAdvisor, Prometheus y
Grafana.

------------------------------------------------------------------------

## 14. Flujo distribuido completo

Un escenario representativo del sistema es:

``` text
Usuario
   │
   ▼
MotorScoring.Web
   │
   ▼
Kong Gateway
   │
   ▼
MotorScoring.Api
   │
   │ evaluación
   ▼
SQL Server
   │
   │
   └────► Notificaciones.Api
              │
              ├────► RabbitMQ
              │         │
              │         ▼
              │   Notification.Worker
              │
              └────► Kafka
                        │
                        ▼
                   Audit.Worker
                        │
                        ▼
                auditoria-scoring.jsonl
```

En paralelo:

``` text
Todos los contenedores
        │
        ▼
     cAdvisor
        │
        ▼
    Prometheus
        │
        ▼
      Grafana
```

------------------------------------------------------------------------

## 15. ¿Por qué esta solución demuestra una arquitectura distribuida?

La solución no es un único proceso que ejecuta todas las
responsabilidades.

Existen múltiples componentes ejecutándose de forma independiente:

-   Web.
-   API de scoring.
-   API de identidad.
-   API de notificaciones.
-   Notification Worker.
-   Audit Worker.
-   Kong.
-   RabbitMQ.
-   Kafka.
-   SQL Server.
-   componentes de observabilidad.

Cada servicio tiene una responsabilidad específica y se comunica
mediante red.

### Independencia de procesos

Los Workers no están embebidos dentro de `Notificaciones.Api`. Son
procesos independientes y pueden iniciarse, detenerse o fallar
independientemente.

### Comunicación por red

La solución utiliza diferentes mecanismos:

``` text
HTTP        -> comunicación síncrona
RabbitMQ    -> mensajería asíncrona
Kafka       -> eventos/auditoría
```

### Fallos parciales

La prueba de detener `Identity` demuestra una propiedad importante de un
sistema distribuido: un componente puede quedar fuera de servicio
mientras otros continúan ejecutándose.

``` text
Identity       DOWN

Scoring.Api    UP
Kafka          UP
RabbitMQ       UP
Workers        UP
Kong           UP
```

El sistema deja de ser tratado como una única aplicación monolítica con
un único estado global.

### Desacoplamiento

RabbitMQ y Kafka permiten que el productor no ejecute directamente el
procesamiento del consumidor.

### Observabilidad centralizada

Aunque los componentes están distribuidos, Prometheus y Grafana permiten
tener una vista central de su comportamiento de infraestructura.

------------------------------------------------------------------------

## 16. Arquitectura distribuida vs arquitectura hexagonal

Estos conceptos no compiten entre sí.

La **arquitectura distribuida** describe cómo se divide y comunica el
sistema a nivel de procesos/servicios.

La **arquitectura hexagonal** organiza internamente determinados
servicios.

Por ejemplo:

``` text
Sistema distribuido
│
├── MotorScoring.Api
│      └── Arquitectura Hexagonal
│
├── Identity.Api
│
├── Notificaciones.Api
│      └── Arquitectura Hexagonal
│
├── Notification.Worker
│
└── Audit.Worker
```

Por tanto, un microservicio puede utilizar arquitectura hexagonal
internamente y, al mismo tiempo, formar parte de una arquitectura
distribuida.

------------------------------------------------------------------------

## 17. Puertos principales

  Componente                          Puerto local
  --------------------------------- --------------
  Kong Gateway                                8000
  MotorScoring.Api                            8080
  MotorScoring.Web                            8081
  MotorScoring.Identity                       8082
  MotorScoring.Notificaciones.Api             8083
  cAdvisor                                    8084
  RabbitMQ Management                        15672
  SQL Server                                  1433
  Prometheus                                  9090
  Grafana                                     3000

Los puertos internos y externos deben revisarse en
`docker-compose.v3.yml` si la configuración cambia.

------------------------------------------------------------------------

## 18. Contenedores de la solución

La V3 contempla los contenedores funcionales:

``` text
motor-scoring-api
motor-scoring-audit-worker
motor-scoring-database
motor-scoring-gateway
motor-scoring-identity
motor-scoring-kafka
motor-scoring-notificaciones-api
motor-scoring-notification-worker
motor-scoring-rabbitmq
motor-scoring-web
```

y los componentes de observabilidad:

``` text
motor-scoring-cadvisor
motor-scoring-prometheus
motor-scoring-grafana
```

------------------------------------------------------------------------

## 19. Validaciones realizadas

Durante la implementación se verificó:

-   compilación independiente de los proyectos de Notificaciones y
    Workers;
-   ejecución de `Notificaciones.Api`;
-   publicación de mensajes hacia RabbitMQ;
-   consumo mediante `Notification.Worker`;
-   publicación de auditoría en Kafka;
-   consumo mediante `Audit.Worker`;
-   generación de `auditoria-scoring.jsonl`;
-   autenticación local;
-   MFA;
-   autenticación Google;
-   autenticación GitHub;
-   asignación automática del rol `Analista` para nuevos usuarios
    externos;
-   autorización mediante roles;
-   enrutamiento HTTP mediante Kong;
-   callbacks OAuth a través de Kong;
-   evaluación de scoring;
-   comunicación Scoring -\> Notificaciones;
-   funcionamiento de cAdvisor;
-   scraping de cAdvisor mediante Prometheus;
-   visualización de métricas en Grafana;
-   CPU por contenedor;
-   RAM por contenedor;
-   tráfico RX/TX;
-   detección de contenedores UP/DOWN;
-   caída y recuperación real de `motor-scoring-identity`.

------------------------------------------------------------------------

## 20. Qué no cubre actualmente la observabilidad

La V3 implementa observabilidad de **infraestructura**, no
instrumentación interna de las aplicaciones.

Actualmente se observa:

``` text
CPU
RAM
Network RX/TX
estado de contenedores
```

No se han agregado métricas de aplicación como:

``` text
latencia por endpoint
cantidad de HTTP 200/400/500
cantidad de evaluaciones
cantidad de solicitudes aprobadas/rechazadas
trazas distribuidas
consumer lag de negocio
métricas internas personalizadas
```

Agregar esas métricas correspondería a una evolución posterior mediante
instrumentación de las aplicaciones, por ejemplo con OpenTelemetry y
métricas específicas de cada tecnología.

------------------------------------------------------------------------

## 21. Resultado final

La evolución realizada transforma el ejercicio en un ecosistema donde
las responsabilidades están distribuidas entre componentes
independientes.

Se incorporaron:

``` text
✓ APIs independientes
✓ Identity y MFA
✓ Workers independientes
✓ RabbitMQ
✓ Kafka
✓ comunicación HTTP
✓ mensajería asíncrona
✓ Kong API Gateway
✓ Docker Compose
✓ cAdvisor
✓ Prometheus
✓ Grafana
✓ monitoreo UP/DOWN
✓ CPU, RAM y Network por contenedor
```

La solución demuestra de forma práctica características fundamentales de
sistemas distribuidos: separación de responsabilidades, comunicación por
red, procesamiento asíncrono, desacoplamiento, fallos parciales, gateway
centralizado y observabilidad.

La arquitectura final no depende de que todos los componentes formen
parte del mismo proceso. Cada servicio cumple una responsabilidad
concreta, puede ejecutarse de manera independiente y participa en el
sistema mediante contratos y mecanismos de comunicación definidos.
