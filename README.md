# Tracking Tiger

Sistema antifraude para tarjetas de debito y credito.

## Requisitos

- [Git](https://git-scm.com/downloads)
- [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0). El proyecto usa `net10.0`, así que una versión anterior (como .NET 8) no sirve.

Para comprobar qué versiones tienes instaladas:

```
dotnet --list-sdks
```

Debe aparecer al menos una línea que empiece con `10.0`.

## Clonar el repositorio

```
git clone https://github.com/greilenny26/Tracking-Tiger.git
cd Tracking-Tiger
```

## Ejecutar

El proyecto es una API web de ASP.NET Core. Para iniciarla:

```
dotnet run
```

La primera vez tarda un poco porque restaura dependencias y compila. Cuando la API está lista, la consola muestra:

```
Now listening on: http://localhost:5000
Application started. Press Ctrl+C to shut down.
```

Para comprobar que responde, abre `http://localhost:5000/` en el navegador. Por ahora la respuesta esperada es **404 (No encontrado)**, porque la API todavía no tiene endpoints. En la consola puede aparecer el aviso `No action descriptors found`; también es esperado mientras no haya controladores.

Para detener la API, presiona `Ctrl+C`.

La primera ejecución de .NET en una máquina también muestra un mensaje de bienvenida sobre telemetría. Es normal y no es un error.

## Notas

- Las carpetas `bin/` y `obj/` se generan al compilar y están ignoradas por el `.gitignore`.
- Pasos verificados en Windows con .NET SDK 10.0.401.