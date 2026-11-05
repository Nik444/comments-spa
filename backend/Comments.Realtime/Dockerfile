# Контекст сборки: папка backend (docker compose build realtime).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Comments.Contracts/Comments.Contracts.csproj Comments.Contracts/
COPY Comments.Realtime/Comments.Realtime.csproj Comments.Realtime/
RUN dotnet restore Comments.Realtime/Comments.Realtime.csproj

COPY Comments.Contracts/ Comments.Contracts/
COPY Comments.Realtime/ Comments.Realtime/
RUN dotnet publish Comments.Realtime/Comments.Realtime.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl нужен только для проверки готовности (healthcheck в docker-compose.yml).
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Comments.Realtime.dll"]
