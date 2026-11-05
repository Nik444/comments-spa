# Контекст сборки: папка backend (docker compose build api).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только проекты: слой с восстановленными пакетами кэшируется, пока файлы проектов не менялись.
COPY Comments.Contracts/Comments.Contracts.csproj Comments.Contracts/
COPY Comments.Api/Comments.Api.csproj Comments.Api/
RUN dotnet restore Comments.Api/Comments.Api.csproj

COPY Comments.Contracts/ Comments.Contracts/
COPY Comments.Api/ Comments.Api/
RUN dotnet publish Comments.Api/Comments.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl нужен только для проверки готовности (healthcheck в docker-compose.yml).
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Папка для вложений принадлежит пользователю приложения, поэтому том Docker получит те же права.
RUN mkdir -p /data/attachments && chown -R $APP_UID /data

COPY --from=build /app .

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Comments.Api.dll"]
