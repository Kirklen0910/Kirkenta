namespace Kirkenta.Helpers.Import
{
    /// <summary>
    /// Configuración completa del wizard de importación de un módulo.
    /// Cada módulo (Clientes, Productos, Proveedores, etc.) define uno.
    /// </summary>
    public class ImportWizardConfig
    {
        /// <summary>
        /// Nombre del módulo en singular/plural para textos.
        /// Ej: "clientes", "productos", "proveedores".
        /// </summary>
        public string NombrePlural { get; set; } = "";

        /// <summary>
        /// Nombre en singular (para mensajes tipo "Importar 1 cliente").
        /// Ej: "cliente", "producto", "proveedor".
        /// </summary>
        public string NombreSingular { get; set; } = "";

        /// <summary>
        /// Módulo del sistema para permisos. Ej: "Ventas", "Inventario", "Compras".
        /// </summary>
        public string ModuloPermiso { get; set; } = "";

        /// <summary>
        /// Submódulo del sistema para permisos. Ej: "ClientesImport", "ProductosImport".
        /// </summary>
        public string SubmoduloPermiso { get; set; } = "";

        /// <summary>
        /// Clave única para la sesión. Ej: "Clientes", "Productos", "Proveedores".
        /// El helper internamente usa "Import{SessionKey}_Headers" y "_Rows".
        /// </summary>
        public string SessionKey { get; set; } = "";

        /// <summary>
        /// URL de la página Index del módulo (botón Cancelar y redirect final).
        /// Ej: "/Clientes/Index".
        /// </summary>
        public string UrlIndex { get; set; } = "";

        /// <summary>
        /// URL de la página Import (paso 1). Ej: "/Clientes/Import".
        /// </summary>
        public string UrlImport { get; set; } = "";

        /// <summary>
        /// URL de la página ImportMap (paso 2). Ej: "/Clientes/ImportMap".
        /// </summary>
        public string UrlImportMap { get; set; } = "";

        /// <summary>
        /// Lista de campos mapeables que verá el usuario.
        /// </summary>
        public List<CampoMapeo> Campos { get; set; } = new();

        /// <summary>
        /// Alias por campo, para autodetección de columnas.
        /// Key = campo.Key, Value = array de alias.
        /// </summary>
        public Dictionary<string, string[]> AliasCampos { get; set; } = new();

        /// <summary>
        /// Mapeo mínimo requerido para proceder con la importación.
        /// Ej: { "Nombre" }. Si alguno falta, se rechaza el POST.
        /// </summary>
        public string[] CamposRequeridos { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Si es true, se ofrece el checkbox de "Crear Categorías automáticamente"
        /// (usado solo por Productos).
        /// </summary>
        public bool OfrecerCrearCategorias { get; set; } = false;

        /// <summary>
        /// Si es true, se ofrece el checkbox de "Crear Impuestos automáticamente"
        /// (usado solo por Productos).
        /// </summary>
        public bool OfrecerCrearImpuestos { get; set; } = false;

        // ===== Helpers de sesión =====
        public string SessionHeadersKey => $"Import{SessionKey}_Headers";
        public string SessionRowsKey => $"Import{SessionKey}_Rows";

        // ===== Helpers de textos =====
        public string TituloPagina => $"Importar {NombrePlural}";
        public string TituloMapa => "Mapear columnas";
    }
}