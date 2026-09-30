FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY MotorScoring.Audit.Worker/ ./MotorScoring.Audit.Worker/
RUN dotnet restore MotorScoring.Audit.Worker/MotorScoring.Audit.Worker.csproj && dotnet publish MotorScoring.Audit.Worker/MotorScoring.Audit.Worker.csproj -c Release -o /app/publish --no-restore
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MotorScoring.Audit.Worker.dll"]
