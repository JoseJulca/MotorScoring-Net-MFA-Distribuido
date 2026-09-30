# Notificaciones distribuidas (.NET 8)

- API hexagonal: Domain -> Application (puertos/caso de uso) <- Infrastructure (publishers RabbitMQ/Kafka), API HTTP como adaptador de entrada.
- Notification.Worker: consumidor RabbitMQ y correo simulado.
- Audit.Worker: consumidor Kafka y auditoría JSONL persistida en volumen Docker.
- Scoring conserva su respuesta HTTP; después del commit notifica por HTTP en modo best-effort.

Ejecutar desde la raíz: `docker compose up --build -d`. Web: http://localhost:8081; Scoring: http://localhost:8080; Identity: http://localhost:8082; Notificaciones: http://localhost:8083/health; RabbitMQ: http://localhost:15672 (usuario scoring; contraseña de RABBITMQ_PASSWORD o scoring-local).

Evaluar una solicitud autenticada desde la Web; consultar `docker compose logs -f motor-scoring-notification-worker motor-scoring-audit-worker` y `docker exec motor-scoring-audit-worker cat /app/audit/auditoria-scoring.jsonl`.

Limitaciones de demostración: el envío HTTP es best-effort y no garantiza entrega ante caídas; si Kafka falla tras confirmar RabbitMQ puede haber publicación parcial. Para producción implementar outbox, idempotencia por EventId, reintentos/DLQ, almacenamiento de auditoría duradero y autenticación entre servicios reforzada. RabbitMQ envía los mensajes fallidos a scoring.notificaciones.email.dlq. Kafka reintenta desde el último offset confirmado y el JSONL puede duplicar registros ante un crash. El correo es simulado y Email llega null desde Scoring porque el contrato de evaluación actual no contiene correo.


## Organización de soluciones

Los workers son aplicaciones independientes y se ubican al mismo nivel que `MotorScoring.Notificaciones`.
La solución `MotorScoring.Notificaciones.sln` contiene únicamente las cuatro capas del API hexagonal.
Cada worker tiene su propio `.sln`, y `../MotorScoring.Completo.sln` incluye todos los proyectos.

Desde la raíz del repositorio:

```powershell
dotnet build MotorScoring.Notificaciones/MotorScoring.Notificaciones.sln
dotnet build MotorScoring.Notification.Worker/MotorScoring.Notification.Worker.sln
dotnet build MotorScoring.Audit.Worker/MotorScoring.Audit.Worker.sln
docker compose up --build -d
```
