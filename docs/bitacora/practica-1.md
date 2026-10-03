# Bitácora — Práctica 1

## Sesión 1: Configuración del entorno de desarrollo y variables de entorno

- **Qué le pedí:** "B8. Variables de entorno en Windows... B11. Commit de preparación" y ayuda para verificar la configuración de `.env.cmd` e ignorado en Git.
- **Qué devolvió:** Explicación del funcionamiento de `call .env.cmd`, corrección del uso de terminal (cambio de PowerShell a CMD), indicación para remover espacios en la clave de aplicación de Gmail, y plantilla del Pull Request para GitHub.
- **Dónde se equivocó:** Sin errores; verifiqué:
  - Que el archivo `.env.cmd` contuviera la sintaxis correcta para CMD (`set CLAVE=VALOR`).
  - Que la contraseña de aplicación de Google no incluyera espacios (`xxxxxxxxxxxxxxxx`).
  - Que la terminal usada para ejecutar `call .env.cmd` fuera Command Prompt (CMD) y no PowerShell.
  - Que `git check-ignore -v .env.cmd` confirmara que la regla `.env.*` en el `.gitignore` bloquea correctamente el archivo sensible.
- **Cómo lo detecté:** Revisé los comandos en la terminal de VS Code y ejecuté `git status` y `git check-ignore` para comprobar que Git no rastreara credenciales.
- **Cómo lo corregí:** Cambié el perfil de la terminal de VS Code de PowerShell a CMD, removí los espacios de la clave de Gmail y corregí el error de tipeo en el comando `git add .gitignore`.
- **Commit(s):** 
  - `491a560` Agrega reglas de commits y requisitos al repositorio (Bitácora)