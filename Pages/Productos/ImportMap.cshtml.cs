using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Import;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Kirkenta.Pages.Productos
{
    public class ImportMapModel : PageModel
    {
        private static readonly ImportWizardConfig Sesion = new() { SessionKey = "Productos" };

        private readonly ApplicationDbContext _context;
        private readonly List<(Producto, decimal)> _stockInicialPendiente = new();
        private int? _usuarioImportId;

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

        public static readonly List<CampoMapeo> CamposDef = new List<CampoMapeo>
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

        private static readonly Dictionary<string, string[]> AliasBase = new()
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

        /// <summary>
        /// Alias de autodetección. Incluye la etiqueta visible de cada campo ("Código de barras",
        /// "Unidad de medida"...) para que los archivos exportados / la plantilla se mapeen exactos.
        /// </summary>
        public static readonly Dictionary<string, string[]> AliasCampos = AliasBase.ToDictionary(
            p => p.Key,
            p => p.Value
                .Append(ColumnMapper.MarcaPropio + ImportWizardConfig.EtiquetaLimpia(
                    CamposDef.FirstOrDefault(c => c.Key == p.Key)?.Label ?? p.Key))
                .ToArray());

        public IActionResult OnGetPlantilla() =>
            File(ImportReportHelper.PlantillaXlsx(CamposDef.Select(c => c.Label)), ImportReportHelper.TipoXlsx, "plantilla_productos.xlsx");

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

            var datos = ImportWizardHelper.RecuperarDeSesion(HttpContext.Session, Sesion);
            if (datos == null)
            {
                TempData["Error"] = "La sesión expiró. Vuelve a subir el archivo.";
                return RedirectToPage("/Productos/Import");
            }

            HeadersArchivo = datos.Value.Headers;
            FilasArchivo = datos.Value.Rows;

            MapaSugerido = ColumnMapper.Detectar(HeadersArchivo, AliasCampos);

            Campos = CamposDef;

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

            var datos = ImportWizardHelper.RecuperarDeSesion(HttpContext.Session, Sesion);
            if (datos == null)
            {
                TempData["Error"] = "La sesión expiró. Vuelve a subir el archivo.";
                return RedirectToPage("/Productos/Import");
            }

            var filas = datos.Value.Rows;
            HeadersArchivo = datos.Value.Headers;

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

            ImportWizardHelper.LimpiarSesion(HttpContext.Session, Sesion);

            // Éxito limpio => mensaje y listado. Con errores/advertencias => pantalla de resultado
            // con el motivo de cada fila y CSV descargable para corregir y reintentar.
            return ImportReportHelper.Finalizar(this, "/Productos/Index", resultado, HeadersArchivo, filas);
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

            _usuarioImportId = _context.Usuarios
                .Where(u => u.Username == User.Identity!.Name)
                .Select(u => (int?)u.Id)
                .FirstOrDefault();

            // ===== 1. Precargar catálogos y existentes =====
            var categorias = _context.Categorias.ToList();
            var impuestos = _context.Impuestos.ToList();
            var unidades = _context.UnidadesMedida.AsNoTracking().ToList();

            var skusExistentes = _context.Productos
                .Where(p => p.SKU != null)
                .Select(p => p.SKU!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var codigosBarrasExistentes = _context.Productos
                .Where(p => p.CodigoBarras != null)
                .Select(p => p.CodigoBarras!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // ===== 2. Serie de SKU y próximo número local =====
            var serieProducto = _context.SeriesDocumentos
                .FirstOrDefault(s => s.Tipo == "Producto" && s.EsPredeterminada && s.Activa);

            string prefijoSerie = serieProducto?.Prefijo ?? "PRO";
            string sepSerie = serieProducto?.Separador ?? "-";
            int longitudSerie = serieProducto?.LongitudNumero ?? 4;
            string? formatoSerie = serieProducto?.FormatoPersonalizado;

            int proximoNumeroLocal = serieProducto?.SiguienteNumero ?? (skusExistentes.Count + 1);

            // Saltar los que ya existen
            while (skusExistentes.Contains(
                FormatearSKU(prefijoSerie, sepSerie, longitudSerie, proximoNumeroLocal, formatoSerie)))
            {
                proximoNumeroLocal++;
            }

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

                    string Etiq(string campo) => ImportWizardConfig.EtiquetaLimpia(
                        CamposDef.FirstOrDefault(c => c.Key == campo)?.Label ?? campo);
                    decimal? Num(string campo) => ImportHelper.Decimal(resultado, filaNum, Etiq(campo), Obtener(campo));

                    var nombre = Obtener("Nombre");
                    if (string.IsNullOrWhiteSpace(nombre))
                    {
                        resultado.AgregarError(filaNum, "El campo 'Nombre' es obligatorio");
                        continue;
                    }

                    if (nombre.Length > 150)
                    {
                        resultado.AgregarError(filaNum, $"El nombre excede 150 caracteres ({nombre.Length})");
                        continue;
                    }

                    var precioVenta = ParserHelper.ParsearDecimal(Obtener("PrecioVenta"));
                    if (precioVenta == null)
                    {
                        resultado.AgregarError(filaNum, "El campo 'Precio venta' es obligatorio y debe ser numérico");
                        continue;
                    }

                    var activo = ImportHelper.Booleano(resultado, filaNum, Etiq("Activo"), Obtener("Activo"));
                    var precioCompra = Num("PrecioCompra");
                    var precioMayorista = Num("PrecioMayorista");
                    var stock = Num("Stock");
                    var stockMinimo = Num("StockMinimo");
                    
                    if ((stock ?? 0) < 0 || (precioCompra ?? 0) < 0 || (precioMayorista ?? 0) < 0 || precioVenta.Value < 0)
                    {
                        resultado.AgregarError(filaNum, "Stock y precios no pueden ser negativos");
                        continue;
                    }

                    var sku = Obtener("SKU");
                    var codigoBarras = Obtener("CodigoBarras");

                    // ===== VALIDACIÓN: SKU máx 50 =====
                    if (!string.IsNullOrEmpty(sku) && sku.Length > 50)
                    {
                        resultado.AgregarAdvertencia(filaNum,
                            $"El SKU '{sku}' excede 50 caracteres. Se ignora y se autogenera.");
                        sku = null;
                    }

                    // ===== VALIDACIÓN: Código de barras máx 50 =====
                    if (!string.IsNullOrEmpty(codigoBarras) && codigoBarras.Length > 50)
                    {
                        resultado.AgregarAdvertencia(filaNum,
                            $"El código de barras '{codigoBarras}' excede 50 caracteres. Se ignora.");
                        codigoBarras = null;
                    }

                    // Buscar existente
                    Producto? existente = null;
                    if (!string.IsNullOrEmpty(sku))
                        existente = _context.Productos.FirstOrDefault(p => p.SKU == sku);
                    if (existente == null && !string.IsNullOrEmpty(codigoBarras))
                        existente = _context.Productos.FirstOrDefault(p => p.CodigoBarras == codigoBarras);

                    // Actualizando: el código de barras nuevo no puede pertenecer a otro producto
                    if (existente != null && !string.IsNullOrEmpty(codigoBarras) &&
                        !string.Equals(codigoBarras, existente.CodigoBarras, StringComparison.OrdinalIgnoreCase) &&
                        codigosBarrasExistentes.Contains(codigoBarras))
                    {
                        resultado.AgregarAdvertencia(filaNum,
                            $"El código de barras '{codigoBarras}' ya lo usa otro producto; no se cambió.");
                        codigoBarras = null;
                    }

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

                    // ===== Duplicados dentro del mismo archivo =====
                    // (antes dos filas nuevas con el mismo SKU o código de barras hacían fallar TODA la importación)
                    if (existente == null)
                    {
                        if (!string.IsNullOrEmpty(sku) && !skusExistentes.Add(sku))
                        {
                            resultado.AgregarAdvertencia(filaNum, $"El SKU '{sku}' está repetido en el archivo; se ignora esta fila.");
                            resultado.Ignorados++;
                            continue;
                        }
                        if (!string.IsNullOrEmpty(codigoBarras) && !codigosBarrasExistentes.Add(codigoBarras))
                        {
                            resultado.AgregarAdvertencia(filaNum, $"El código de barras '{codigoBarras}' está repetido en el archivo; se ignora esta fila.");
                            resultado.Ignorados++;
                            continue;
                        }
                    }

                    // ===== Autogenerar SKU si no viene o si fue rechazado =====
                    if (string.IsNullOrEmpty(sku) && existente == null)
                    {
                        do
                        {
                            sku = FormatearSKU(prefijoSerie, sepSerie, longitudSerie, proximoNumeroLocal, formatoSerie);
                            proximoNumeroLocal++;
                        }
                        while (skusExistentes.Contains(sku));

                        skusExistentes.Add(sku);
                    }

                    // Resolver categoría e impuesto
                    int? categoriaId = ResolverCategoria(Obtener("Categoria"), categorias, crearCategoriasAuto, resultado, filaNum);
                    int? impuestoId = ResolverImpuesto(Obtener("Impuesto"), impuestos, crearImpuestosAuto, resultado, filaNum);

                    // ===== Unidad de medida: se resuelve contra el catálogo (nombre o abreviatura) =====
                    var unidad = Obtener("UnidadMedida");
                    if (!string.IsNullOrEmpty(unidad))
                    {
                        var unidadNorm = ColumnMapper.Normalizar(unidad);
                        var enCatalogo = unidades.FirstOrDefault(u => ColumnMapper.Normalizar(u.Nombre) == unidadNorm)
                                         ?? unidades.FirstOrDefault(u => ColumnMapper.Normalizar(u.Abreviatura) == unidadNorm);
                        if (enCatalogo != null)
                        {
                            unidad = enCatalogo.Nombre;
                        }
                        else
                        {
                            resultado.AgregarAdvertencia(filaNum,
                                $"La unidad de medida '{unidad}' no está en el catálogo de unidades; se guardó tal cual.");
                        }
                        if (unidad.Length > 20)
                        {
                            resultado.AgregarAdvertencia(filaNum, "La unidad de medida excede 20 caracteres; se recortó.");
                            unidad = unidad.Substring(0, 20);
                        }
                    }

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
                        // El stock nunca se pisa directo: el cambio queda en el kardex
                        if (stock.HasValue)
                            InventarioHelper.Ajustar(_context, existente, stock.Value, "Importacion", _usuarioImportId,
                                "Ajuste por importación de productos");
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
                            Stock = 0,
                            StockMinimo = stockMinimo ?? 0,
                            UnidadMedida = string.IsNullOrEmpty(unidad) ? "Unidad" : unidad,
                            Activo = activo ?? true,
                            FechaCreacion = DateTime.Now
                        };

                        _context.Productos.Add(producto);
                        if ((stock ?? 0) > 0) _stockInicialPendiente.Add((producto, stock!.Value));
                        resultado.Creados++;
                    }
                }
                catch (Exception ex)
                {
                    resultado.AgregarError(filaNum, $"Error inesperado: {ex.Message}");
                }
            }

            // ===== 3. Guardar de una sola vez (todo o nada) =====
            ImportHelper.GuardarSeguro(_context, resultado, () =>
            {
                _context.SaveChanges();

                // Stock inicial de productos nuevos: entra por el kardex (ya tienen Id)
                if (_stockInicialPendiente.Count > 0)
                {
                    foreach (var (prod, cant) in _stockInicialPendiente)
                        InventarioHelper.Registrar(_context, prod, cant, "StockInicial", null, prod.Id,
                            _usuarioImportId, prod.PrecioCompra, "Stock inicial por importación");
                    _context.SaveChanges();
                }

                // ===== 4. Sincronizar la serie =====
                if (serieProducto != null && resultado.Creados > 0)
                {
                    serieProducto.SiguienteNumero = proximoNumeroLocal;
                    _context.SaveChanges();
                }
            });
            _stockInicialPendiente.Clear();

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

            if (valor.Length > 100)
                resultado.AgregarAdvertencia(filaNum, $"El nombre de la categoría '{valor}' excede 100 caracteres; se recortó.");

            var nueva = new Categoria
            {
                Nombre = valor.Length > 100 ? valor.Substring(0, 100) : valor,
                Activa = true,
                FechaCreacion = DateTime.Now
            };
            _context.Categorias.Add(nueva);
            _context.SaveChanges();
            categorias.Add(nueva);
            return nueva.Id;
        }

        private int? ResolverImpuesto(string? valor, List<Impuesto> impuestos, bool crear, ImportResult resultado, int filaNum)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;

            var nombreNormalizado = ColumnMapper.Normalizar(valor);
            var existente = impuestos.FirstOrDefault(i => ColumnMapper.Normalizar(i.Nombre) == nombreNormalizado);
            if (existente != null) return existente.Id;

            var porcentajeValor = ParserHelper.ParsearDecimal(valor);
            if (porcentajeValor.HasValue)
            {
                var porPorcentaje = impuestos.FirstOrDefault(i => i.Porcentaje == porcentajeValor.Value);
                if (porPorcentaje != null) return porPorcentaje.Id;
            }

            if (!crear)
            {
                resultado.AgregarAdvertencia(filaNum, $"Impuesto '{valor}' no existe (no se creó)");
                return null;
            }

            // Nunca se crea un impuesto "inventado" al 0 %: sin un porcentaje claro, no se crea
            if (!porcentajeValor.HasValue || porcentajeValor < 0 || porcentajeValor > 100)
            {
                resultado.AgregarAdvertencia(filaNum,
                    $"Impuesto '{valor}' no existe y no trae un porcentaje válido para crearlo; se dejó sin impuesto.");
                return null;
            }

            var nuevo = new Impuesto
            {
                Nombre = valor.Length > 50 ? valor.Substring(0, 50) : valor,
                Porcentaje = porcentajeValor.Value,
                Activo = true
            };
            _context.Impuestos.Add(nuevo);
            _context.SaveChanges();
            impuestos.Add(nuevo);
            return nuevo.Id;
        }

        /// <summary>
        /// Formatea un SKU respetando el formato de la serie.
        /// Trunca a 50 caracteres por seguridad.
        /// </summary>
        private static string FormatearSKU(
            string prefijo, string separador, int longitud, int numero, string? formatoPersonalizado)
        {
            var numeroStr = numero.ToString().PadLeft(longitud, '0');
            var anio = DateTime.Now.Year.ToString();
            var mes = DateTime.Now.Month.ToString("D2");
            var dia = DateTime.Now.Day.ToString("D2");

            string resultado;

            if (!string.IsNullOrWhiteSpace(formatoPersonalizado))
            {
                resultado = formatoPersonalizado
                    .Replace("{PREFIX}", prefijo)
                    .Replace("{PREFIJO}", prefijo)
                    .Replace("{SUFFIX}", "")
                    .Replace("{SUFIJO}", "")
                    .Replace("{SEP}", separador)
                    .Replace("{YEAR}", anio)
                    .Replace("{ANIO}", anio)
                    .Replace("{MONTH}", mes)
                    .Replace("{MES}", mes)
                    .Replace("{DAY}", dia)
                    .Replace("{DIA}", dia)
                    .Replace("{NUM}", numeroStr);
            }
            else
            {
                resultado = $"{prefijo}{separador}{numeroStr}";
            }

            if (resultado.Length > 50)
                resultado = resultado.Substring(0, 50);

            return resultado;
        }
    }
}