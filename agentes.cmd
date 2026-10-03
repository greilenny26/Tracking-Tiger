@echo off
TITLE Tracking Tiger - Agente Antifraude
CLS

echo ========================================================
echo   Iniciando Agente Tracking Tiger (ASP.NET Core / EF Core)
echo ========================================================

:: 1. Verificación de entorno .NET
where dotnet >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    echo [ERROR] .NET SDK no está instalado o no se encuentra en el PATH.
    exit /b 1
)

:: 2. Invocación de Claude Code
claude %*