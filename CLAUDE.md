# Reglas del proyecto (Tracking Tiger, Práctica 1)
- Stack: C# / ASP.NET Core Web API, EF Core + SQLite, MailKit para SMTP.
- Idioma: mensajes al usuario, commits y docs en español.
- NUNCA trabajes directo en main. Una rama por funcionalidad: feat/<nombre>.
- Commits atómicos, asunto en imperativo + ID del requisito.
  Ejemplo: "Agrega hash con sal para contraseñas (RF-CA-02)".
- No mezcles cambios sin relación en un commit.
- Contraseñas: hash con sal (nunca texto plano). Tokens (activación, recuperación,
  sesión): guardar solo su hash; un solo uso; con vencimiento.
- Respuestas idénticas exista o no el correo (activación, recuperación).
- Login fallido: mismo mensaje para correo inexistente y contraseña incorrecta.
- Exigencia de rol declarada en UN solo punto legible; se valida en el servidor.
- Los errores al usuario nunca exponen trazas ni consultas (RD-08).
- Correo: nunca se envía dentro de la operación; se registra en CorreoEnCola
  y un proceso aparte lo envía (idempotente).
- Credenciales SMTP solo por variables de entorno. Nada de secretos en el repo.
- Antes de dar algo por terminado: compila (`dotnet build`) y explica cómo probarlo.
- No avances a otra funcionalidad sin que yo lo pida.

## Habilidades Disponibles (Skills)
- `skills.cmd build`: Verifica compilación con `dotnet build`.
- `skills.cmd check-secrets`: Escanea credenciales expuestas en archivos de configuración.
- `skills.cmd clean`: Limpia los ejecutables y archivos temporales.
- `skills.cmd db-migrate <nombre>`: Aplica cambios en la base de datos SQLite con EF Core.
- `skills.cmd test`: Ejecuta las pruebas automatizadas del proyecto.
- `skills.cmd git-feature <nombre>`: Crea una nueva rama `feat/<nombre>` para la funcionalidad.