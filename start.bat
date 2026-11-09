@echo off
rem Local start of the whole project in Docker: site http://localhost:8080
setlocal
cd /d "%~dp0"

docker info >nul 2>&1
if errorlevel 1 (
  echo Docker is not running. Start Docker Desktop and run this file again.
  pause
  exit /b 1
)

rem Local start must never go to a remote server (for example Azure context)
docker context use default >nul

echo Building and starting. The first run takes a few minutes...
docker compose up -d --build --wait --wait-timeout 300
if errorlevel 1 (
  echo.
  echo Start failed. See the messages above. Logs: docker compose logs --tail 50
  pause
  exit /b 1
)

echo.
echo Ready: http://localhost:8080  (Swagger: /swagger, GraphQL: /graphql)
start "" http://localhost:8080
pause
