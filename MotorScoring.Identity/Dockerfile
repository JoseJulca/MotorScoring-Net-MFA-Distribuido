FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/MotorScoring.Identity.Domain/MotorScoring.Identity.Domain.csproj", "src/MotorScoring.Identity.Domain/"]
COPY ["src/MotorScoring.Identity.Application/MotorScoring.Identity.Application.csproj", "src/MotorScoring.Identity.Application/"]
COPY ["src/MotorScoring.Identity.Adapters.Inbound.Api/MotorScoring.Identity.Adapters.Inbound.Api.csproj", "src/MotorScoring.Identity.Adapters.Inbound.Api/"]
COPY ["src/MotorScoring.Identity.Adapters.Outbound.Identity/MotorScoring.Identity.Adapters.Outbound.Identity.csproj", "src/MotorScoring.Identity.Adapters.Outbound.Identity/"]
COPY ["src/MotorScoring.Identity.Api/MotorScoring.Identity.Api.csproj", "src/MotorScoring.Identity.Api/"]
RUN dotnet restore "src/MotorScoring.Identity.Api/MotorScoring.Identity.Api.csproj"
COPY . .
RUN dotnet publish "src/MotorScoring.Identity.Api/MotorScoring.Identity.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8082
EXPOSE 8082
ENTRYPOINT ["dotnet", "MotorScoring.Identity.dll"]
