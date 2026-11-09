@echo off
rem Stops the local project. Data (database, files) is kept.
rem To delete the data too, run in this folder: docker compose down -v
setlocal
cd /d "%~dp0"

docker context use default >nul
docker compose down
pause
