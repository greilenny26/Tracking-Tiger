# Etapa 1: compila con el SDK. Nada de esta etapa llega a la imagen final.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilacion
WORKDIR /fuente
# Primero solo el .csproj: si el código cambia pero los paquetes no, Docker reutiliza la restauración.
COPY src/TrackingTiger/TrackingTiger.csproj src/TrackingTiger/
RUN dotnet restore src/TrackingTiger/TrackingTiger.csproj
COPY src/ src/
RUN dotnet publish src/TrackingTiger/TrackingTiger.csproj -c Release -o /publicado --no-restore

# Etapa 2: imagen final, solo con el runtime y la aplicación publicada.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=compilacion /publicado .
# /datos es donde se monta el volumen de la base de datos (RD-09). La aplicación no corre como root.
RUN mkdir -p /datos && chown "$APP_UID" /datos
USER $APP_UID
EXPOSE 8080
# Crea el Administrador inicial si no existe (RF-CA-04) y arranca el servidor. Va aquí y no en un script .sh
# para que un clon en Windows, que convierte los saltos de línea a CRLF, no rompa el arranque.
ENTRYPOINT ["sh", "-c", "dotnet TrackingTiger.dll crear-admin; exec dotnet TrackingTiger.dll"]
