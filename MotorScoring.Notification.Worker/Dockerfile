FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY MotorScoring.Notification.Worker/ ./MotorScoring.Notification.Worker/
RUN dotnet restore MotorScoring.Notification.Worker/MotorScoring.Notification.Worker.csproj && dotnet publish MotorScoring.Notification.Worker/MotorScoring.Notification.Worker.csproj -c Release -o /app/publish --no-restore
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MotorScoring.Notification.Worker.dll"]
