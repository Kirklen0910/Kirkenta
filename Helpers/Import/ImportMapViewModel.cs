namespace Kirkenta.Helpers.Import
{
    /// <summary>
    /// Datos que necesita la tabla de mapeo compartida (Pages/Shared/_ImportMapTable.cshtml).
    /// Sirve para todos los módulos, sin importar qué clase CampoMapeo usen.
    /// </summary>
    public class ImportMapViewModel
    {
        public List<ImportMapCampo> Campos { get; set; } = new();
        public List<string> Headers { get; set; } = new();
        public List<Dictionary<string, string>> Filas { get; set; } = new();
        public Dictionary<string, string?> Sugerido { get; set; } = new();

        public int TotalFilas => Filas.Count;

        public static ImportMapViewModel Crear(
            IEnumerable<ImportMapCampo> campos,
            List<string> headers,
            List<Dictionary<string, string>> filas,
            Dictionary<string, string?> sugerido,
            Dictionary<string, string[]>? alias = null)
        {
            var lista = campos.ToList();

            foreach (var campo in lista)
            {
                var header = sugerido.GetValueOrDefault(campo.Key);
                string[]? aliasCampo = null;
                alias?.TryGetValue(campo.Key, out aliasCampo);
                campo.SugeridoExacto = !string.IsNullOrEmpty(header)
                    && ColumnMapper.EsCoincidenciaExacta(header, campo.Key, campo.Label, aliasCampo);
            }

            return new ImportMapViewModel
            {
                Campos = lista,
                Headers = headers,
                Filas = filas,
                Sugerido = sugerido
            };
        }

        /// <summary>Para los módulos que usan Helpers.Import.CampoMapeo.</summary>
        public static ImportMapViewModel Crear(
            IEnumerable<CampoMapeo> campos,
            List<string> headers,
            List<Dictionary<string, string>> filas,
            Dictionary<string, string?> sugerido,
            Dictionary<string, string[]>? alias = null)
        {
            return Crear(
                campos.Select(c => new ImportMapCampo
                {
                    Key = c.Key,
                    Label = c.Label,
                    Requerido = c.Requerido,
                    Ayuda = c.Ayuda
                }),
                headers, filas, sugerido, alias);
        }
    }

    public class ImportMapCampo
    {
        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
        public bool Requerido { get; set; }
        public string? Ayuda { get; set; }

        /// <summary>True si la columna sugerida coincide exactamente con el nombre/alias del campo.</summary>
        public bool SugeridoExacto { get; set; }
    }
}
