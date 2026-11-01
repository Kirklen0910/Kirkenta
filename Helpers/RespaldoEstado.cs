namespace Kirkenta.Helpers
{
    /// <summary>Resumen de la carpeta de respaldos (los .zip que crea tools/respaldo-bd.ps1).</summary>
    public record EstadoRespaldo(string? Carpeta, DateTime? Ultimo, long UltimoBytes, int Cantidad);

    /// <summary>Nivel de aviso según qué tan viejo es el último respaldo.</summary>
    public record AvisoRespaldo(string Nivel, string Texto);

    public static class RespaldoEstado
    {
        /// <summary>
        /// Busca la carpeta "respaldos" junto al proyecto (la que usa el script por defecto) y lee el .zip más reciente.
        /// Solo cuenta .zip: el script borra el .sql y deja únicamente respaldos verificados.
        /// </summary>
        public static EstadoRespaldo Leer(string? carpeta = null)
        {
            carpeta ??= Buscar();
            if (carpeta == null || !Directory.Exists(carpeta)) return new EstadoRespaldo(carpeta, null, 0, 0);

            try
            {
                var zips = new DirectoryInfo(carpeta).GetFiles("*.zip").Where(f => f.Length >= 512).OrderByDescending(f => f.LastWriteTime).ToList();
                if (zips.Count == 0) return new EstadoRespaldo(carpeta, null, 0, 0);
                return new EstadoRespaldo(carpeta, zips[0].LastWriteTime, zips[0].Length, zips.Count);
            }
            catch
            {
                return new EstadoRespaldo(carpeta, null, 0, 0);
            }
        }

        /// <summary>Decide si hay que avisar. null = todo bien (respaldo de las últimas 36 horas).</summary>
        public static AvisoRespaldo? Evaluar(DateTime? ultimo, DateTime ahora)
        {
            if (ultimo == null)
                return new AvisoRespaldo("danger", "Todavía no hay ningún respaldo de la base de datos. Programa el respaldo diario (tools\\programar-respaldo.ps1).");

            var horas = (ahora - ultimo.Value).TotalHours;
            if (horas <= 36) return null;
            var dias = (int)Math.Floor(horas / 24);
            return new AvisoRespaldo(dias >= 3 ? "danger" : "warning",
                $"El último respaldo de la base de datos fue hace {dias} día(s) ({ultimo.Value:dd/MM/yyyy HH:mm}). Revisa la tarea programada.");
        }

        private const string ArchivoError = "ultimo-error-respaldo.txt";

        /// <summary>Anota que un respaldo falló (para avisar en pantalla y en el asistente).</summary>
        public static void RegistrarError(string carpeta, string mensaje)
        {
            try { File.WriteAllText(Path.Combine(carpeta, ArchivoError), DateTime.Now.ToString("o") + "\n" + mensaje); } catch { }
        }

        /// <summary>Borra la marca de error (se llama cuando un respaldo sale bien).</summary>
        public static void LimpiarError(string carpeta)
        {
            try { var f = Path.Combine(carpeta, ArchivoError); if (File.Exists(f)) File.Delete(f); } catch { }
        }

        /// <summary>Error del último respaldo, si lo hubo y no se ha corregido con un respaldo exitoso posterior.</summary>
        public static (DateTime Fecha, string Mensaje)? LeerError(string? carpeta = null)
        {
            try
            {
                carpeta ??= Buscar();
                if (carpeta == null) return null;
                var f = Path.Combine(carpeta, ArchivoError);
                if (!File.Exists(f)) return null;
                var lineas = File.ReadAllText(f).Split('\n', 2);
                var fecha = DateTime.TryParse(lineas[0], null, System.Globalization.DateTimeStyles.RoundtripKind, out var d) ? d : File.GetLastWriteTime(f);
                return (fecha, lineas.Length > 1 ? lineas[1].Trim() : "");
            }
            catch { return null; }
        }

        /// <summary>Aviso único para el panel, Seguridad y el asistente: primero un fallo reciente, luego la antigüedad del último respaldo.</summary>
        public static AvisoRespaldo? Avisar(DateTime ahora)
        {
            var err = LeerError();
            if (err != null)
                return new AvisoRespaldo("danger", $"El respaldo automático falló el {err.Value.Fecha:dd/MM/yyyy HH:mm}: {err.Value.Mensaje}");
            return Evaluar(Leer().Ultimo, ahora);
        }

        public static string? Buscar()
        {
            var inicios = new List<string> { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
            foreach (var inicio in inicios)
            {
                var dir = new DirectoryInfo(inicio);
                for (int i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
                {
                    var cand = Path.Combine(dir.FullName, "respaldos");
                    if (Directory.Exists(cand)) return cand;
                }
            }
            return null;
        }
    }
}
