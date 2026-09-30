# Estructura distribuida sin biblioteca compartida de contratos

- `MotorScoring.Notificaciones`: API en arquitectura hexagonal; contrato del evento en `Application/Contracts`.
- `MotorScoring.Notification.Worker`: ejecutable independiente, contrato JSON local en `Models`.
- `MotorScoring.Audit.Worker`: ejecutable independiente, contrato JSON local en `Models`.
- Los tres contratos tienen el mismo esquema JSON para mantener compatibilidad sin referencias entre proyectos.
- Cada Worker tiene su propio `.sln` con un solo proyecto.
- `MotorScoring.Completo.sln` contiene todos los proyectos, sin `MotorScoring.Messaging.Contracts`.
- Docker Compose mantiene los servicios y los Workers se compilan sin copiar bibliotecas compartidas.

## Compilar

```powershell
dotnet build MotorScoring.Notification.Worker/MotorScoring.Notification.Worker.sln
dotnet build MotorScoring.Audit.Worker/MotorScoring.Audit.Worker.sln
dotnet build MotorScoring.Notificaciones/MotorScoring.Notificaciones.sln
dotnet build MotorScoring.Completo.sln
docker compose up --build -d
```
