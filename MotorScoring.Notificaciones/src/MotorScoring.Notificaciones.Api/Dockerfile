FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore MotorScoring.Notificaciones/src/MotorScoring.Notificaciones.Api/MotorScoring.Notificaciones.Api.csproj && dotnet publish MotorScoring.Notificaciones/src/MotorScoring.Notificaciones.Api/MotorScoring.Notificaciones.Api.csproj -c Release -o /app/publish --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MotorScoring.Notificaciones.Api.dll"]
