FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["MotorScoring.Web.csproj", "./"]
RUN dotnet restore "MotorScoring.Web.csproj"

COPY . .
RUN dotnet publish "MotorScoring.Web.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8081
EXPOSE 8081

ENTRYPOINT ["dotnet", "MotorScoring.Web.dll"]