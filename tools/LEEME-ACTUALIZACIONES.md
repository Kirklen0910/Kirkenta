# Cómo actualizar la base de datos sin perder información

## Qué hace solo el sistema (ActualizacionBD)
Cada vez que arranca, compara el modelo (`Models/`) contra la base real y, SOLO AGREGANDO:

1. Si hay algo pendiente y la base ya tiene datos, crea antes un respaldo `..._previo-actualizacion.zip`
   (nunca lo borra la retención). Si ese respaldo falla, no toca nada y reintenta al siguiente arranque.
2. Crea las tablas nuevas (con sus índices).
3. Agrega las columnas nuevas. Si son NOT NULL y no traen DEFAULT, les pone uno seguro (0, '', fecha actual...).
4. Crea los índices normales de las columnas nuevas.
5. Ejecuta los **pasos manuales** pendientes (`ActualizacionBD.PasosManuales`), una sola vez cada uno.

Lo que hace queda en el log con el prefijo `[Arranque] ActualizacionBD`.
**Nunca** hace DROP, RENAME, MODIFY, CHANGE, TRUNCATE ni DELETE.

## Reglas al cambiar el modelo (para no perder datos ni romper respaldos viejos)
- Agregar tablas o columnas: libre. Si la columna nueva es obligatoria, ponle un valor por defecto sensato
  (`[Required]` + valor inicial en la propiedad, o `HasDefaultValue`) para que quede claro qué traen los registros viejos.
- **No renombres ni borres** columnas/tablas en una actualización normal: el sistema no lo hace y los respaldos
  viejos dejarían de calzar. Si ya no se usa, déjala. Si de verdad hay que migrar, hazlo en DOS versiones:
  v1 agrega la nueva y copia los datos (paso manual); v2 (meses después) deja de usar la vieja. Nunca se borra la vieja solo.
- No cambies el tipo de una columna con datos (ej. int → varchar). Crea una columna nueva y copia con un paso manual.
- Las llaves foráneas y los índices únicos nuevos sobre tablas con datos NO se crean solos; se hacen con paso manual
  (porque pueden fallar si los datos viejos no cumplen).

## Paso manual (rellenar datos de registros viejos, sembrar un registro, etc.)
Agrega al FINAL de `Helpers/ActualizacionBD.cs` → `PasosManuales`:

```csharp
new PasoManual {
    Id = "2026-11-01-001-estado-por-defecto",      // único, no se repite ni se edita jamás
    Descripcion = "Pone Estado='Activo' a clientes viejos",
    Sql = new[] { "UPDATE `Clientes` SET `Estado` = 'Activo' WHERE `Estado` = ''" }
}
```
- Debe ser aditivo e idempotente (poder correr 2 veces sin daño). Solo se permite ALTER TABLE ... ADD, CREATE TABLE/INDEX,
  INSERT [IGNORE] y UPDATE con WHERE. Todo lo demás se rechaza.
- Si un paso falla, se detiene y se reintenta en el próximo arranque (se ve en el log).
- Al restaurar un respaldo se borra el registro de pasos y se reaplican al reiniciar (por eso deben ser idempotentes).

## Restaurar un respaldo viejo en un sistema más nuevo
Funciona: las columnas que el respaldo no trae quedan con su DEFAULT y las tablas nuevas quedan intactas.
Un respaldo de una versión MÁS NUEVA que el programa se rechaza (primero actualiza el sistema).

## Antes de entregar una versión
Corre `.\tools\nueva-actualizacion.ps1 -Version X.Y.Z` (compila, corre pruebas y genera el instalador).
Prueba el .exe en una PC de prueba con una copia de la base de un cliente. Pide siempre el respaldo al cliente antes.

## Apagar el actualizador
`Mantenimiento:ActualizarEsquemaAutomatico=false` lo desactiva. `Mantenimiento:RespaldoAntesDeActualizar=false` quita el respaldo previo (no recomendado).
