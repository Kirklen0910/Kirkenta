using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Import;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Kirkenta.Pages.Productos
{
    public class ImportMapModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ImportMapModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<string> HeadersArchivo { get; set; } = new();
        public List<Dictionary<string, string>> FilasArchivo { get; set; } = new();
        public Dictionary<string, string?> MapaSugerido { get; set; } = new();
        public List<CampoMapeo> Campos { get; set; } = new();

        public class CampoMapeo
        {
            public string Key { get; set; } = "";
            public string Label { get; set; } = "";
            public bool Requerido { get; set; }
        }

        private static readonly Dictionary<string, string[]> AliasCampos = new()
        {
            { "SKU",           new[] { "sku", "codigo", "code", "cod", "clave", "codigoproducto" } },
            { "CodigoBarras",  new[] { "codigobarras", "barras", "barcode", "ean", "upc" } },
            { "Nombre",        new[] { "nombre", "name", "producto", "descripcioncorta", "articulo" } },
            { "Descripcion",   new[] { "descripcion", "description", "detalle", "observaciones" } },
            { "Categoria",     new[] { "categoria", "category", "rubro", "familia", "linea" } },
            { "Impuesto",      new[] { "impuesto", "tax", "iva" } },
            { "UnidadMedida",  new[] { "unidadmedida", "unidad", "um", "unit", "medida", "presentacion" } },
            { "PrecioCompra",  new[] { "preciocompra", "costo", "cost", "preciocosto", "compra", "precioc" } },
            { "PrecioVenta",   new[] { "precioventa", "pvp", "precio", "price", "venta", "preciov" } },
            { "PrecioMayorista", new[] { "preciomayorista", "mayorista", "preciomay", "wholesale" } },
            { "Stock",         new[] { "stock", "existencia", "cantidad", "qty", "inventario", "disponible" } },
            { "StockMinimo",   new[] { "stockminimo", "minimo", "minstock", "min" } },
            { "Activo",        new[] { "activo", "active", "estado", "status" } },
        };

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "ProductosImport", "crear"))
            {
                TempData["Error"] = "No tienes permiso para importar productos";
                return RedirectToPage("/Productos/Index");
            }

            var jsonHeaders = HttpContext.Session.GetString("ImportProductos_Headers");
            var jsonRows = HttpContext.Session.GetString("ImportProductos_Rows");

            if (string.IsNullOrEmpty(jsonHeaders) || string.IsNullOrEmpty(jsonRows))
            {
                TempData["Error"] = "La sesión expiró. Vuelve a subir el archivo.";
                return RedirectToPage("/Productos/Import");
            }

            HeadersArchivo = JsonSerializer.Deserialize<List<string>>(jsonHeaders) ?? new();
            FilasArchivo = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(jsonRows) ?? new();

            MapaSugerido = ColumnMapper.Detectar(HeadersArchivo, AliasCampos);

            Campos = new List<CampoMapeo>
            {
                new() { Key = "SKU",              Label = "SKU",                 Requerido = false },
                new() { Key = "Nombre",           Label = "Nombre",              Requerido = true },
                new() { Key = "Descripcion",      Label = "Descripción",         Requerido = false },
                new() { Key = "CodigoBarras",     Label = "Código de barras",    Requerido = false },
                new() { Key = "Categoria",        Label = "Categoría",           Requerido = false },
                new() { Key = "Impuesto",         Label = "Impuesto",            Requerido = false },
                new() { Key = "UnidadMedida",     Label = "Unidad de medida",    Requerido = false },
                new() { Key = "PrecioCompra",     Label = "Precio compra",       Requerido = false },
                new() { Key = "PrecioVenta",      Label = "Precio venta",        Requerido = true },
                new() { Key = "PrecioMayorista",  Label = "Precio mayorista",    Requerido = false },
                new() { Key = "Stock",            Label = "Stock",               Requerido = false },
                new() { Key = "StockMinimo",      Label = "Stock mínimo",        Requerido = false },
                new() { Key = "Activo",           Label = "Activo (Sí/No)",      Requerido = false },
            };

            return Page();
        }

        [BindProperty]
        public Dictionary<string, string> Mapeo { get; set; } = new();

        [BindProperty]
        public string ModoImportacion { get; set; } = "upsert";

        [BindProperty]
        public bool CrearCategoriasAuto { get; set; } = true;

        [BindProperty]
        public bool CrearImpuestosAuto { get; set; } = false;

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "ProductosImport", "crear"))
            {
                TempData["Error"] = "No tienes permiso para importar productos";
                return RedirectToPage("/Productos/Index");
            }

            var jsonHeaders = HttpContext.Session.GetString("ImportProductos_Headers");
            var jsonRows = HttpContext.Session.GetString("ImportProductos_Rows");

            if (string.IsNullOrEmpty(jsonHeaders) || string.IsNullOrEmpty(jsonRows))
            {
                TempData["Error"] = "La sesión expiró. Vuelve a subir el archivo.";
                return RedirectToPage("/Productos/Import");
            }

            var filas = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(jsonRows) ?? new();

            if (string.IsNullOrEmpty(Mapeo.GetValueOrDefault("Nombre")))
            {
                TempData["Error"] = "Debes mapear al menos el campo 'Nombre'";
                return RedirectToPage("/Productos/ImportMap");
            }

            if (string.IsNullOrEmpty(Mapeo.GetValueOrDefault("PrecioVenta")))
            {
                TempData["Error"] = "Debes mapear el campo 'Precio venta'";
                return RedirectToPage("/Productos/ImportMap");
            }

            var resultado = ImportarProductos(filas, Mapeo, ModoImportacion, currentUser?.Id,
                CrearCategoriasAuto, CrearImpuestosAuto);

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Importar productos",
                $"Importó productos: {resultado.Resumen()}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            HttpContext.Session.Remove("ImportProductos_Headers");
            HttpContext.Session.Remove("ImportProductos_Rows");

            TempData["Success"] = $"Importación completada: {resultado.Resumen()}";
            return RedirectToPage("/Productos/Index");
        }

        private ImportResult ImportarProductos(
            List<Dictionary<string, string>> filas,
            Dictionary<string, string> mapa,
            string modo,
            int? usuarioId,
            bool crearCategoriasAuto,
            bool crearImpuestosAuto)
        {
            var resultado = new ImportResult { TotalFilas = filas.Count };

            // Precargar catálogos
            var categorias = _context.Categorias.ToList();
            var impuestos = _context.Impuestos.ToList();
            var skusExistentes = _context.Productos
                .Where(p => p.SKU != null)
                .Select(p => p.SKU!)
                .ToHashSet();
            var codigosBarrasExistentes = _context.Productos
                .Where(p => p.CodigoBarras != null)
                .Select(p => p.CodigoBarras!)
                .ToHashSet();

            int filaNum = 1;
            foreach (var fila in filas)
            {
                filaNum++;
                try
                {
                    string? Obtener(string campo)
                    {
                        var header = mapa.GetValueOrDefault(campo);
                        if (string.IsNullOrEmpty(header)) return null;
                        return fila.GetValueOrDefault(header)?.Trim();
                    }

                    var nombre = Obtener("Nombre");
                    if (string.IsNullOrWhiteSpace(nombre))
                    {
                        resultado.AgregarError(filaNum, "El campo 'Nombre' es obligatorio");
                        continue;
                    }

                    var precioVenta = ParsearDecimal(Obtener("PrecioVenta"));
                    if (precioVenta == null)
                    {
                        resultado.AgregarError(filaNum, "El campo 'Precio venta' es obligatorio y debe ser numérico");
                        continue;
                    }

                    var sku = Obtener("SKU");
                    var codigoBarras = Obtener("CodigoBarras");

                    // Buscar existente (por SKU o código de barras)
                    Producto? existente = null;
                    if (!string.IsNullOrEmpty(sku))
                        existente = _context.Productos.FirstOrDefault(p => p.SKU == sku);
                    if (existente == null && !string.IsNullOrEmpty(codigoBarras))
                        existente = _context.Productos.FirstOrDefault(p => p.CodigoBarras == codigoBarras);

                    if (existente != null && modo == "crear")
                    {
                        resultado.Ignorados++;
                        continue;
                    }

                    if (existente == null && modo == "actualizar")
                    {
                        resultado.AgregarAdvertencia(filaNum, $"Producto '{nombre}' no existe, se ignora (modo: solo actualizar)");
                        resultado.Ignorados++;
                        continue;
                    }

                    // Autogenerar SKU si no viene
                    if (string.IsNullOrEmpty(sku) && existente == null)
                    {
                        sku = NumeroDocumentoHelper.GenerarSiguiente(_context, "Producto");
                    }

                    // Resolver categoría
                    int? categoriaId = ResolverCategoria(Obtener("Categoria"), categorias, crearCategoriasAuto, resultado, filaNum);
                    // Resolver impuesto
                    int? impuestoId = ResolverImpuesto(Obtener("Impuesto"), impuestos, crearImpuestosAuto, resultado, filaNum);

                    var activo = ParsearBool(Obtener("Activo"));
                    var precioCompra = ParsearDecimal(Obtener("PrecioCompra"));
                    var precioMayorista = ParsearDecimal(Obtener("PrecioMayorista"));
                    var stock = ParsearDecimal(Obtener("Stock"));
                    var stockMinimo = ParsearDecimal(Obtener("StockMinimo"));
                    var unidad = Obtener("UnidadMedida");

                    if (existente != null)
                    {
                        existente.Nombre = nombre;
                        existente.Descripcion = Obtener("Descripcion") ?? existente.Descripcion;
                        existente.CodigoBarras = codigoBarras ?? existente.CodigoBarras;
                        existente.CategoriaId = categoriaId ?? existente.CategoriaId;
                        existente.ImpuestoId = impuestoId ?? existente.ImpuestoId;
                        existente.PrecioCompra = precioCompra ?? existente.PrecioCompra;
                        existente.PrecioVenta = precioVenta.Value;
                        existente.PrecioMayorista = precioMayorista ?? existente.PrecioMayorista;
                        existente.Stock = stock ?? existente.Stock;
                        existente.StockMinimo = stockMinimo ?? existente.StockMinimo;
                        if (!string.IsNullOrEmpty(unidad)) existente.UnidadMedida = unidad;
                        existente.Activo = activo ?? existente.Activo;
                        resultado.Actualizados++;
                    }
                    else
                    {
                        var producto = new Producto
                        {
                            SKU = sku,
                            CodigoBarras = codigoBarras,
                            Nombre = nombre,
                            Descripcion = Obtener("Descripcion"),
                            CategoriaId = categoriaId,
                            ImpuestoId = impuestoId,
                            PrecioCompra = precioCompra ?? 0,
                            PrecioVenta = precioVenta.Value,
                            PrecioMayorista = precioMayorista ?? 0,
                            Stock = stock ?? 0,
                            StockMinimo = stockMinimo ?? 0,
                            UnidadMedida = string.IsNullOrEmpty(unidad) ? "Unidad" : unidad,
                            Activo = activo ?? true,
                            FechaCreacion = DateTime.Now
                        };

                        _context.Productos.Add(producto);
                        resultado.Creados++;
                    }
                }
                catch (Exception ex)
                {
                    resultado.AgregarError(filaNum, $"Error inesperado: {ex.Message}");
                }
            }

            _context.SaveChanges();
            return resultado;
        }

        private int? ResolverCategoria(string? valor, List<Categoria> categorias, bool crear, ImportResult resultado, int filaNum)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;

            var nombreNormalizado = ColumnMapper.Normalizar(valor);
            var existente = categorias.FirstOrDefault(c => ColumnMapper.Normalizar(c.Nombre) == nombreNormalizado);
            if (existente != null) return existente.Id;

            if (!crear)
            {
                resultado.AgregarAdvertencia(filaNum, $"Categoría '{valor}' no existe (no se creó)");
                return null;
            }

            var nueva = new Categoria
            {
                Nombre = valor,
                Activa = true,
                FechaCreacion = DateTime.Now
            };
            _context.Categorias.Add(nueva);
            _context.SaveChanges(); // Para obtener el Id
            categorias.Add(nueva);
            return nueva.Id;
        }

        private int? ResolverImpuesto(string? valor, List<Impuesto> impuestos, bool crear, ImportResult resultado, int filaNum)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;

            var nombreNormalizado = ColumnMapper.Normalizar(valor);
            var existente = impuestos.FirstOrDefault(i => ColumnMapper.Normalizar(i.Nombre) == nombreNormalizado);
            if (existente != null) return existente.Id;

            // Intenta parsear como porcentaje numérico
            if (decimal.TryParse(valor.Replace("%", "").Trim(), out var porcentaje))
            {
                var porPorcentaje = impuestos.FirstOrDefault(i => i.Porcentaje == porcentaje);
                if (porPorcentaje != null) return porPorcentaje.Id;
            }

            if (!crear)
            {
                resultado.AgregarAdvertencia(filaNum, $"Impuesto '{valor}' no existe (no se creó)");
                return null;
            }

            var nuevo = new Impuesto
            {
                Nombre = valor,
                Porcentaje = decimal.TryParse(valor.Replace("%", "").Trim(), out var p) ? p : 0,
                Activo = true
            };
            _context.Impuestos.Add(nuevo);
            _context.SaveChanges();
            impuestos.Add(nuevo);
            return nuevo.Id;
        }

        private static bool? ParsearBool(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;

            var normalizado = valor.Trim().ToLowerInvariant();
            if (normalizado == "1" || normalizado == "true" || normalizado == "sí" || normalizado == "si" ||
                normalizado == "yes" || normalizado == "y" || normalizado == "activo" || normalizado == "activa")
                return true;
            if (normalizado == "0" || normalizado == "false" || normalizado == "no" ||
                normalizado == "n" || normalizado == "inactivo" || normalizado == "inactiva")
                return false;

            return null;
        }

        private static decimal? ParsearDecimal(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            var limpio = valor.Trim()
                .Replace(",", "")
                .Replace("L.", "")
                .Replace("$", "")
                .Replace("%", "")
                .Trim();

            return decimal.TryParse(limpio, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : null;
        }
    }
}