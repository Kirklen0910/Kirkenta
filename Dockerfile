# ============================================================
# Kirkenta ERP - imagen de producción (sin código fuente: solo binarios compilados)
#   docker compose up -d --build        (ver docker-compose.yml y tools\LEEME-DOCKER-Y-LICENCIAS.md)
# ============================================================

# ---------- 1) Compilación ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Kirkenta.csproj ./
RUN dotnet restore Kirkenta.csproj

COPY . .

# Las licencias deben tener su clave pública puesta; si no, la imagen quedaría sin protección.
# (Solo para pruebas internas: docker build --build-arg PERMITIR_SIN_LICENCIA=true .)
ARG PERMITIR_SIN_LICENCIA=false
RUN if grep -q 'Valor = ""' Services/Licencia/LicenciaClavePublica.cs && [ "$PERMITIR_SIN_LICENCIA" != "true" ]; then \
      echo "ERROR: falta la clave publica de licencias (Services/Licencia/LicenciaClavePublica.cs). Ejecuta Licenciador llaves y pega el resultado."; \
      exit 1; \
    fi

# La imagen NUNCA lleva la contraseña de la base de datos de desarrollo: se vacía la cadena de conexión
# y se recibe por la variable de entorno ConnectionStrings__DefaultConnection.
RUN sed -i -E 's#("DefaultConnection": *")[^"]*(")#\1\2#' appsettings.json

RUN dotnet publish Kirkenta.csproj -c Release -o /out --no-restore \
      -p:DebugType=none -p:DebugSymbols=false -p:UseAppHost=false

# ---------- 2) Ejecución ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Hora de Honduras (el sistema usa la hora local para fechas de ventas, cierres y respaldos)
ENV TZ=America/Tegucigalpa \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_gcServer=0

COPY --from=build /out .

# Carpetas con datos del cliente (se montan como volúmenes para que sobrevivan a las actualizaciones)
RUN mkdir -p /app/App_Data /app/logs /app/respaldos /app/wwwroot/uploads \
 && chown -R $APP_UID:$APP_UID /app/App_Data /app/logs /app/respaldos /app/wwwroot/uploads
VOLUME ["/app/App_Data", "/app/logs", "/app/respaldos", "/app/wwwroot/uploads"]

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Kirkenta.dll"]
