# Kirkenta como servidor local (en el negocio, sin depender de la nube)

Kirkenta corre en una PC del negocio (el **servidor**) y las demás PCs, tablets y celulares entran por la red WiFi/cable.
Todo funciona aunque se caiga el internet. Lo único que usa internet es la IA opcional y el aviso por correo.

## 1. Preparar HTTPS (una sola vez)

Sin HTTPS no se puede iniciar sesión desde otros equipos (las cookies seguras lo exigen) y el POS sin internet no se activa.

1. En el servidor abre **PowerShell como administrador** en la carpeta del proyecto.
2. Ejecuta:

       powershell -ExecutionPolicy Bypass -File .\tools\https-local.ps1

3. El script crea el certificado, abre el puerto en el firewall y te muestra las direcciones, por ejemplo `https://192.168.1.10:5115`.
4. Para que los demás equipos no muestren la advertencia roja, instala en cada uno el archivo `certs\kirkenta-local.cer`
   (Windows: doble clic → Instalar → *Entidades de certificación raíz de confianza*; Android: Ajustes → Seguridad → Instalar certificado de CA;
   iPhone: abrir el archivo, instalar el perfil y activarlo en Ajustes → General → Información → Config. de certificados).

> Dale una **IP fija** al servidor en el router (reserva DHCP) para que la dirección no cambie.
> Si la red de Windows está como "Pública", cámbiala a "Privada" o el firewall bloqueará la entrada.

## 2. Arrancar en modo servidor

    tools\iniciar-servidor.cmd

Guarda el registro en `logs\servidor.log`. En el navegador del servidor entra a `http://localhost:5114`; desde los otros equipos, a `https://IP-DEL-SERVIDOR:5115`.

## 3. Que arranque solo al prender la PC

    powershell -ExecutionPolicy Bypass -File .\tools\instalar-arranque-automatico.ps1

- Arranca al iniciar sesión y se reinicia solo si se cae. Para quitarlo: `-Quitar`.
- Deja el inicio de sesión de Windows automático (`netplwiz`) y, en el BIOS, activa *Restore on AC Power Loss* para que la PC prenda sola tras un apagón.
- MySQL ya arranca solo como servicio de Windows.

## 4. Respaldos

En **Configuración → Respaldos del sistema** define la hora del respaldo diario y una **copia externa** (USB, otro disco o carpeta de red).
Si el USB no está conectado, el respaldo normal no se afecta y el sistema te avisa.

## 5. Impresora de tickets

En **Configuración → Impresora de tickets** conecta la térmica (USB por nombre de Windows, o por red con IP). Abre la gaveta de dinero al imprimir.
La impresora debe estar conectada al servidor o a la red del servidor.

## Problemas comunes

| Síntoma | Causa y solución |
|---|---|
| El celular no abre la dirección | Revisa que esté en el mismo WiFi, que el servidor tenga la IP correcta y que el puerto 5115 esté permitido en el firewall (el script lo hace). |
| Entra pero no deja iniciar sesión | Estás entrando por `http://` desde otro equipo. Usa siempre `https://IP:5115`. |
| Advertencia de certificado | Instala `certs\kirkenta-local.cer` en ese equipo (paso 1.4). |
| El POS no guarda ventas sin internet | El modo sin conexión solo funciona con HTTPS o en `localhost`. |
| Cambió la IP del servidor | Vuelve a correr `https-local.ps1` (genera un certificado nuevo con las IP actuales) y reinstala el `.cer`. |
