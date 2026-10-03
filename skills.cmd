@echo off
:: ========================================================
:: Habilidades del Proyecto Tracking Tiger (Práctica 1)
:: ========================================================

IF "%1"=="" GOTO Help
IF /I "%1"=="build" GOTO BuildProject
IF /I "%1"=="check-secrets" GOTO CheckSecrets
IF /I "%1"=="clean" GOTO CleanProject
IF /I "%1"=="db-migrate" GOTO DbMigrate
IF /I "%1"=="test" GOTO RunTests
IF /I "%1"=="git-feature" GOTO GitFeature
GOTO Unknown

:BuildProject
    echo [SKILL] Compilando la solucion ASP.NET Core...
    dotnet build
    EXIT /B %ERRORLEVEL%

:CheckSecrets
    echo [SKILL] Escaneando codigo para evitar secretos o claves SMTP hardcodeadas...
    findstr /S /I /C:"Password=" /C:"Smtp:" /C:"Secret" *.cs appsettings*.json
    echo [INFO] Revision completada. Verifica que no haya credenciales expuestas arriba.
    EXIT /B 0

:CleanProject
    echo [SKILL] Limpiando carpetas bin y obj...
    dotnet clean
    EXIT /B 0

:DbMigrate
    echo [SKILL] Generando y aplicando migracion de EF Core...
    IF "%2"=="" (
        echo [ERROR] Debes especificar un nombre para la migracion. Ejemplo: .\skills.cmd db-migrate AddUserTable
        EXIT /B 1
    )
    dotnet ef migrations add %2
    dotnet ef database update
    EXIT /B %ERRORLEVEL%

:RunTests
    echo [SKILL] Ejecutando pruebas unitarias e integracion...
    dotnet test
    EXIT /B %ERRORLEVEL%

:GitFeature
    IF "%2"=="" (
        echo [ERROR] Indica el nombre de la funcionalidad. Ejemplo: .\skills.cmd git-feature auth-tokens
        EXIT /B 1
    )
    echo [SKILL] Creando y cambiando a la rama feat/%2...
    git checkout -b feat/%2
    EXIT /B 0

:Unknown
    echo [ERROR] La habilidad "%1" no existe.
    GOTO Help

:Help
    echo.
    echo Uso de skills.cmd:
    echo   skills.cmd build               - Compila el proyecto con 'dotnet build'
    echo   skills.cmd check-secrets       - Revisa que no haya contraseñas expuestas
    echo   skills.cmd clean               - Limpia los archivos de compilación
    echo   skills.cmd db-migrate [nombre] - Crea y aplica una migración de EF Core
    echo   skills.cmd test                - Corre las pruebas con 'dotnet test'
    echo   skills.cmd git-feature [nombre]- Crea la rama feat/[nombre] respetando las reglas
    EXIT /B 1