# Kirkenta: entrega sin código fuente y licencias de uso

## 1. Cómo se entrega

**Opción A – Docker (recomendada).** El cliente recibe la imagen compilada (sin código fuente) y se levanta con un solo comando; trae su propia base MySQL.
**Opción B – Ejecutable de Windows.** `tools\publicar-ejecutable.ps1` genera una carpeta con `Kirkenta.exe` (no necesita instalar .NET). El cliente instala MySQL aparte.

En ambos casos el cliente usa el sistema desde el navegador, igual que hoy.

> Honestidad sobre la protección: el código viaja compilado, pero cualquier programa compilado de .NET se puede "descompilar" con herramientas gratuitas. Esto impide ver el código cómodamente, no es blindaje total. Para subir el nivel se puede ofuscar el resultado (Obfuscar, ConfuserEx o similares) y, sobre todo, apoyarse en el contrato de uso. Las licencias están firmadas criptográficamente: **no se pueden fabricar** sin tu llave privada.

### Docker – pasos (una sola vez en el equipo del cliente o en el tuyo para probar)
1. Instalar Docker Desktop (Windows) o Docker Engine (Linux).
2. Copiar `.env.example` a `.env` y cambiar las claves (solo letras y números).
3. `docker compose up -d --build`
4. Abrir `http://localhost:8080`. La primera vez crea las tablas y el usuario `admin`; su clave está en
   `docker compose exec kirkenta cat App_Data/primer-acceso.txt` (o define `ADMIN_CLAVE` en `.env`). Cámbiala dentro del sistema.
5. Actualizar a una versión nueva: `docker compose up -d --build` (los datos viven en volúmenes y no se pierden).

Para entregar **sin código fuente** al cliente: construye la imagen en tu equipo (`docker build -t kirkenta:1.0 .`), expórtala (`docker save kirkenta:1.0 -o kirkenta-1.0.tar`) y entrégale el `.tar` + `docker-compose.yml` (cambiando `build: .` por `image: kirkenta:1.0`) + `.env.example`. Él ejecuta `docker load -i kirkenta-1.0.tar` y `docker compose up -d`.

**HTTPS para otros equipos de la red** (POS sin internet y cámara lo exigen): en `.env` pon `COOKIE_SOLO_HTTPS=true` y `HOST_HTTPS=<IP del servidor>`, y levanta con `docker compose --profile https up -d`. Luego `docker compose cp https:/data/caddy/pki/authorities/local/root.crt ./caddy-root.crt` e instala ese archivo como "entidad de certificación raíz de confianza" en cada equipo/celular. Se entra por `https://<IP>`.

**Copias de seguridad:** el respaldo del sistema se guarda en el volumen `kk-respaldos`. Para sacarlo a otro disco: `docker compose cp kirkenta:/app/respaldos ./respaldos`.

## 2. Licencias de uso

Funcionan **sin internet**. Tú las emites con `tools\licenciador.cmd` (menú). Tipos: licencias cortas de 30 minutos, 1, 3 o 12 horas (para pruebas rápidas); demo de 7, 15 o 30 días; 6 meses, 1, 3 o 5 años; sin vencimiento.

### Configuración inicial (una sola vez, en TU computadora)
1. `tools\licenciador.cmd llaves` → crea `%USERPROFILE%\KirkentaLicencias\llave-privada.pem` y muestra el contenido para `Services\Licencia\LicenciaClavePublica.cs`. **Pega ese contenido en el archivo y recompila.**
2. **Haz copia de seguridad de `llave-privada.pem`** (si la pierdes no podrás emitir más licencias para las instalaciones existentes; si la roban, pueden fabricar licencias). Nunca va dentro del proyecto ni de la imagen.
3. Mientras la clave pública esté vacía el sistema NO pide licencia (modo desarrollo). El Dockerfile y `publicar-ejecutable.ps1` se niegan a empaquetar si sigue vacía.

### Emitir una licencia
1. El cliente instala el sistema; sin licencia solo ve la pantalla **Licencia**, que muestra el **código de la computadora**.
2. Te manda ese código. Ejecutas `tools\licenciador.cmd`, opción 2, eliges el plan, el nombre del cliente y pegas el código.
3. Le envías el texto `KRK1-...` (queda también en `%USERPROFILE%\KirkentaLicencias\licencias-emitidas\` con un `registro.csv`).
4. El cliente lo pega en la pantalla Licencia → activa. Al renovar, el administrador pega la licencia nueva en Configuración → Licencia.

### Cómo se protege contra el reloj y las trampas
- La licencia está ligada al código de la computadora: la misma licencia no sirve en otra.
- El plazo de una demo cuenta con el calendario real desde que se activa: si la máquina estuvo apagada 6 días de 7, al encender ya queda 1 día; si estuvo apagada 8, está vencida.
- El sistema recuerda la fecha más avanzada que ha visto y la guarda firmada en 4 lugares (carpeta de datos, ProgramData, perfil del usuario y la base de datos). Si el reloj aparece por detrás de esa fecha (más de 5 minutos), **se bloquea** hasta que entregues una licencia nueva. Cambiar el reloj hacia adelante solo acorta la licencia.
- Cada licencia trae una fecha límite para activarla (demo: 3 días; normal: 30). Así no se pueden guardar licencias para usarlas después ni reiniciar una demo reactivando la misma.
- Borrar los registros de la licencia también bloquea (si ya pasó su plazo de activación).

### Casos a tener en cuenta
- Si a una PC se le agota la batería de la BIOS y la fecha vuelve a 2000, el sistema se bloqueará (parece retroceso). Se resuelve emitiendo otra licencia; para no regalar tiempo a un cliente de pago, emítela con `--inicio AAAA-MM-DD` (la fecha original de inicio) y el vencimiento no cambia. Ejemplo: `tools\licenciador.cmd emitir --plan 1a --cliente "X" --maquina ABCD-1234-ABCD-1234 --inicio 2026-11-01`
- Licencia "libre" (`--libre`) sirve en cualquier computadora: úsala solo si aceptas que se copie.
- Ningún sistema offline es 100% a prueba de alguien muy técnico que modifique el programa. Esto frena el uso normal de trampas (reloj, copiar archivos, reinstalar) y las licencias no se pueden falsificar.

## 3. Instalador `Setup.exe` para clientes con Windows (sistema + MySQL en un solo paso)

`tools\crear-instalador.ps1` genera `instalador\salida\Kirkenta-Setup-<version>.exe`. Es lo único que se lleva al cliente.

**En tu computadora (una vez):** instala Inno Setup 6 (gratis, jrsoftware.org), baja el **ZIP** de MySQL Community Server 8.0 para Windows de 64 bits (no el MSI) y, si puedes, `vc_redist.x64.exe` y cópialo en `instalador\recursos\`.

**Crear el instalador:** `.\tools\crear-instalador.ps1 -MySqlZip C:\ruta\mysql-8.0.xx-winx64.zip -Version 1.0.0` (las veces siguientes basta `-Version 1.0.1`).

**Qué hace en la PC del cliente:** copia todo a `C:\Kirkenta`, instala un MySQL propio (puerto 3307, solo accesible desde esa PC, con claves generadas al azar), crea las tablas y el usuario `admin`, deja el sistema arrancando solo al encender la PC (y levantándose si se cae), abre el puerto 5114 para otros equipos de la red y crea el acceso directo "Kirkenta ERP" en el escritorio. Al terminar muestra el usuario y la clave de la primera vez.

**Actualizar a una versión nueva:** se ejecuta el nuevo Setup.exe encima; no toca la base de datos ni los datos del cliente. **Desinstalar** quita el sistema y los servicios pero conserva `C:\Kirkenta\datos-mysql` y `sistema\App_Data` (los datos del cliente).

**Licencia de MySQL:** MySQL Community es GPL. Entregarlo junto a tu sistema como un programa aparte es práctica habitual, pero si lo vas a vender a gran escala conviene revisarlo con un abogado (o usar MariaDB/licencia comercial de Oracle).

Para entrar desde otras PCs/celulares: `http://IP-DEL-SERVIDOR:5114`. (El POS sin internet y la cámara requieren HTTPS: ver `tools\https-local.ps1` o el perfil `https` de Docker.)
