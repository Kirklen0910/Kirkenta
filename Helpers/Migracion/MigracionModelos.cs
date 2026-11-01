namespace Kirkenta.Helpers.Migracion
{
    /// <summary>Una tabla encontrada en el archivo del sistema viejo.</summary>
    public class TablaOrigen
    {
        public string Nombre { get; set; } = "";
        public List<string> Headers { get; set; } = new();
        public List<Dictionary<string, string>> Rows { get; set; } = new();
        /// <summary>Qué se hizo con la tabla al adaptar la base («su número se copió a ventas»…).</summary>
        public string? Nota { get; set; }
        /// <summary>Sus datos ya se pasaron a otra tabla: no se migra sola.</summary>
        public bool Unida { get; set; }
    }

    public class ResultadoMigracionLectura
    {
        public List<TablaOrigen> Tablas { get; } = new();
        public List<string> Avisos { get; } = new();

        /// <summary>Agrega una tabla (si tiene datos) con nombre único.</summary>
        public void Agregar(string nombre, List<string> headers, List<Dictionary<string, string>> rows)
        {
            if (headers.Count == 0 || rows.Count == 0) return;
            nombre = string.IsNullOrWhiteSpace(nombre) ? "Tabla" : nombre.Trim();
            var candidato = nombre;
            int n = 2;
            while (Tablas.Any(t => string.Equals(t.Nombre, candidato, StringComparison.OrdinalIgnoreCase)))
                candidato = $"{nombre} ({n++})";
            Tablas.Add(new TablaOrigen { Nombre = candidato, Headers = headers, Rows = rows });
        }
    }

    /// <summary>Módulo destino al que se puede enviar una tabla (usa el asistente de importación existente).</summary>
    public class EntidadMigracion
    {
        public string Key { get; init; } = "";
        public string Nombre { get; init; } = "";
        public int Orden { get; init; }
        public string UrlImportMap { get; init; } = "";
        public string SessionKey { get; init; } = "";
        /// <summary>Nombres típicos de tabla (normalizados) en otros sistemas.</summary>
        public string[] NombresTabla { get; init; } = Array.Empty<string>();
        public Dictionary<string, string[]> Alias { get; init; } = new();
        public string[] Requeridos { get; init; } = Array.Empty<string>();
        /// <summary>Campos que, si aparecen, delatan la entidad.</summary>
        public string[] Distintivos { get; init; } = Array.Empty<string>();
    }

    public class DeteccionTabla
    {
        public TablaOrigen Tabla { get; set; } = new();
        public int Indice { get; set; }
        public EntidadMigracion? Entidad { get; set; }
        public int Puntaje { get; set; }
        /// <summary>Alta / Media / Baja / Sin sugerencia</summary>
        public string Confianza { get; set; } = "Sin sugerencia";
        public Dictionary<string, string?> Mapa { get; set; } = new();
        public string? PosibleFuturo { get; set; }
        /// <summary>Si la tabla parece historial/saldos: cxc | cxp | ventas | kardex.</summary>
        public string? TipoHistorial { get; set; }
    }
}
