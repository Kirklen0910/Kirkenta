namespace Kirkenta.Helpers.Import
{
    /// <summary>
    /// Define un campo mapeable del sistema (ej: "Nombre", "RTN", "PrecioVenta").
    /// El wizard de importación usa esto para generar la UI de mapeo.
    /// </summary>
    public class CampoMapeo
    {
        /// <summary>
        /// Clave interna del campo (sin espacios). Ej: "RazonSocial", "PrecioVenta".
        /// </summary>
        public string Key { get; set; } = "";

        /// <summary>
        /// Etiqueta visible al usuario. Ej: "Razón social", "Precio de venta".
        /// </summary>
        public string Label { get; set; } = "";

        /// <summary>
        /// Si es true, el usuario DEBE mapearlo (no puede ignorarlo).
        /// </summary>
        public bool Requerido { get; set; }

        /// <summary>
        /// Texto de ayuda opcional (se muestra debajo del selector).
        /// </summary>
        public string? Ayuda { get; set; }
    }
}