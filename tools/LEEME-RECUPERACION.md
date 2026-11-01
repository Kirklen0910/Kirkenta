# Recuperación de Kirkenta si el servidor se daña

## Qué hay de respaldo
| Qué | Script | Cada cuánto | Contiene |
|---|---|---|---|
| Base de datos | `tools\respaldo-bd.ps1` (tarea diaria con `programar-respaldo.ps1`) | Diario | Solo MySQL (verificado) |
| **Sistema completo** | `tools\respaldo-completo.ps1` (tarea semanal con `programar-respaldo-completo.ps1`) | Semanal | MySQL + `wwwroot\uploads` + `App_Data` + `storage` + imágenes + código fuente + manifiesto con suma de verificación |

El `.zip` completo queda en `respaldos-completos\`. **Si queda solo en el mismo equipo no sirve de nada ante un daño del equipo**: usa `-CopiaExterna` (otro disco, USB, carpeta sincronizada de la nube) para que se copie y verifique solo.

`appsettings.json` NO se incluye por defecto porque trae la clave de MySQL. Si quieres incluirlo: `-IncluirConfiguracion` (y guarda ese zip en un lugar privado).

## Hacer un respaldo completo ahora
```
powershell -ExecutionPolicy Bypass -File .\tools\respaldo-completo.ps1 -ArchivoOpciones "C:\Kirkenta-secretos\mysql-respaldo.cnf" -CopiaExterna "E:\RespaldosKirkenta"
```

## Programarlo cada semana
```
powershell -ExecutionPolicy Bypass -File .\tools\programar-respaldo-completo.ps1 -CopiaExterna "E:\RespaldosKirkenta"
```

## Recuperarse en un equipo nuevo
1. Instalar **MySQL Server** y el **SDK de .NET 10**.
2. Copiar el `kirkenta-completo-*.zip` y la carpeta `tools` (viene dentro del zip, en `codigo\tools`; también la tienes en GitHub si la subes).
3. Ver qué trae sin cambiar nada:
   ```
   powershell -ExecutionPolicy Bypass -File .\restaurar-sistema.ps1 -Archivo "E:\RespaldosKirkenta\kirkenta-completo-AAAAMMDD-HHMMSS.zip"
   ```
4. Restaurar código, archivos y base de datos:
   ```
   powershell -ExecutionPolicy Bypass -File .\restaurar-sistema.ps1 -Archivo "...zip" -DestinoProyecto "C:\Kirkenta" -RestaurarBd -Usuario root
   ```
   No pisa una base que ya tenga tablas, salvo que agregues `-Reemplazar` y escribas `RESTAURAR`.
5. Revisar la cadena de conexión en `appsettings.json` y correr `dotnet run` dentro de `C:\Kirkenta`.
6. Entrar al sistema: revisar **Seguridad → Respaldo**, y si usabas IA con clave de API, volver a escribirla (la clave cifrada no se puede abrir en otro equipo).

## Practica antes de necesitarlo
Una vez al mes, corre `tools\probar-restauracion.ps1` (prueba el respaldo de la base en una base temporal) y, cada trimestre, haz una restauración completa en otro equipo o carpeta.
