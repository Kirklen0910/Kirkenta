using System.Text.RegularExpressions;
using Kirkenta.Data;
using Kirkenta.Helpers.Import;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;
using static Kirkenta.Helpers.Migracion.MigracionHistorialHelper;

namespace Kirkenta.Helpers.Migracion
{
    /// <summary>
    /// Importación automática de los catálogos (impuestos, unidades, categorías, productos con stock inicial,
    /// clientes y proveedores) para la migración. Solo CREA lo que no existe (si algo ya está, se salta),
    /// así que se puede repetir sin duplicar. El stock inicial entra por el kardex.
    /// </summary>
    public static class MigracionCatalogosHelper
    {
        public static readonly string[] Orden =
        {
            "PlanCuentas", "MetodosPago", "CuentasFinancieras",
            "Impuestos", "UnidadesMedida", "Categorias", "Productos", "Clientes", "Proveedores", "Empleados"
        };

        public static string Nombre(string key) => key switch
        {
            "Impuestos" => "Impuestos",
            "UnidadesMedida" => "Unidades de medida",
            "Categorias" => "Categorías",
            "Productos" => "Productos / inventario (con stock inicial)",
            "Clientes" => "Clientes",
            "Proveedores" => "Proveedores",
            _ => MigracionOperacionesHelper.Nombre(key)
        };

        public static List<CampoDef> Campos(string key) => key switch
        {
            "Impuestos" => new()
            {
                new() { Key = "Nombre", Label = "Nombre", Requerido = true, Alias = new[] { "nombre", "name", "impuesto", "tax", "descripcion" } },
                new() { Key = "Porcentaje", Label = "Porcentaje", Requerido = true, Alias = new[] { "porcentaje", "tasa", "rate", "isv", "porcent", "valor", "percent" } },
                new() { Key = "Descripcion", Label = "Descripción", Alias = new[] { "detalle", "observaciones" } },
                new() { Key = "Activo", Label = "Activo", Alias = new[] { "activo", "active", "estado", "status", "habilitado" } },
            },
            "UnidadesMedida" => new()
            {
                new() { Key = "Nombre", Label = "Nombre", Requerido = true, Alias = new[] { "nombre", "name", "unidad", "medida", "descripcion" } },
                new() { Key = "Abreviatura", Label = "Abreviatura", Alias = new[] { "abreviatura", "abrev", "abbr", "sigla", "short", "simbolo", "codigo" } },
                new() { Key = "Activa", Label = "Activa", Alias = new[] { "activa", "activo", "active", "estado", "status" } },
            },
            "Categorias" => new()
            {
                new() { Key = "Nombre", Label = "Nombre", Requerido = true, Alias = new[] { "nombre", "name", "categoria", "rubro", "familia", "linea", "grupo", "descripcion" } },
                new() { Key = "Descripcion", Label = "Descripción", Alias = new[] { "detalle", "observaciones" } },
                new() { Key = "Color", Label = "Color", Alias = new[] { "color", "hex" } },
                new() { Key = "Activa", Label = "Activa", Alias = new[] { "activa", "activo", "active", "estado", "status" } },
            },
            "Productos" => new()
            {
                new() { Key = "SKU", Label = "SKU / código", Alias = new[] { "sku", "codigo", "code", "cod", "clave", "codigoproducto", "codigoarticulo", "referencia", "itemcode" } },
                new() { Key = "CodigoBarras", Label = "Código de barras", Alias = new[] { "codigobarras", "barras", "barcode", "ean", "upc", "codbarra" } },
                new() { Key = "Nombre", Label = "Nombre", Requerido = true, Alias = new[] { "nombre", "name", "producto", "articulo", "descripcion", "item", "nombreproducto" } },
                new() { Key = "Descripcion", Label = "Descripción", Alias = new[] { "detalle", "observaciones", "descripcionlarga", "marca", "brand" } },
                new() { Key = "Categoria", Label = "Categoría", Alias = new[] { "categoria", "category", "rubro", "familia", "linea", "grupo", "departamento" } },
                new() { Key = "Impuesto", Label = "Impuesto", Alias = new[] { "impuesto", "tax", "iva", "isv" } },
                new() { Key = "UnidadMedida", Label = "Unidad de medida", Alias = new[] { "unidadmedida", "unidad", "um", "unit", "medida", "presentacion", "uom" } },
                new() { Key = "PrecioCompra", Label = "Costo / precio de compra", Alias = new[] { "preciocompra", "costo", "cost", "preciocosto", "costopromedio", "costounitario", "ultimocosto" } },
                new() { Key = "PrecioVenta", Label = "Precio de venta", Alias = new[] { "precioventa", "pvp", "precio", "price", "preciov", "precio1", "preciopublico" } },
                new() { Key = "PrecioMayorista", Label = "Precio mayorista", Alias = new[] { "preciomayorista", "mayorista", "preciomay", "wholesale", "precio2" } },
                new() { Key = "Stock", Label = "Existencia (stock inicial)", Alias = new[] { "stock", "existencia", "existencias", "cantidad", "qty", "inventario", "disponible", "onhand" } },
                new() { Key = "StockMinimo", Label = "Stock mínimo", Alias = new[] { "stockminimo", "minimo", "minstock", "existenciaminima", "reorden" } },
                new() { Key = "Activo", Label = "Activo", Alias = new[] { "activo", "active", "estado", "status", "habilitado" } },
            },
            "Clientes" => Terceros(false),
            "Proveedores" => Terceros(true),
            _ => MigracionOperacionesHelper.Campos(key)
        };

        private static List<CampoDef> Terceros(bool proveedor)
        {
            var l = new List<CampoDef>
            {
                new() { Key = "Codigo", Label = "Código", Alias = new[] { "codigo", "code", "cod", "clave", proveedor ? "idproveedor" : "idcliente" } },
                new() { Key = "Nombre", Label = "Nombre", Requerido = true, Alias = new[] { "nombre", "name", proveedor ? "proveedor" : "cliente", "razonsocial", "nombrecomercial" } },
                new() { Key = "RazonSocial", Label = "Razón social", Alias = new[] { "razonsocial", "nombrelegal", "razon" } },
                new() { Key = "RTN", Label = "RTN", Alias = new[] { "rtn", "ruc", "nit", "taxid", "identificacion", "identidad" } },
                new() { Key = "Email", Label = "Email", Alias = new[] { "email", "correo", "mail", "correoelectronico" } },
                new() { Key = "Telefono", Label = "Teléfono", Alias = new[] { "telefono", "tel", "phone", "celular", "movil" } },
                new() { Key = "Direccion", Label = "Dirección", Alias = new[] { "direccion", "address", "domicilio" } },
                new() { Key = "Ciudad", Label = "Ciudad", Alias = new[] { "ciudad", "city", "municipio" } },
                new() { Key = "DiasCredito", Label = "Días de crédito", Alias = new[] { "diascredito", "dias", "plazo", "creditodias", "diasplazo" } },
                new() { Key = "LimiteCredito", Label = "Límite de crédito", Alias = new[] { "limitecredito", "limite", "creditlimit", "limitedecredito" } },
                new() { Key = "Notas", Label = "Notas", Alias = new[] { "notas", "notes", "observaciones", "comentarios" } },
                new() { Key = "Activo", Label = "Activo", Alias = new[] { "activo", "active", "estado", "status" } },
            };
            if (proveedor)
            {
                l.Insert(3, new CampoDef { Key = "Contacto", Label = "Persona de contacto", Alias = new[] { "contacto", "contact", "vendedor", "representante" } });
                l.Insert(4, new CampoDef { Key = "TelefonoContacto", Label = "Teléfono del contacto", Alias = new[] { "telefonocontacto", "telcontacto", "celularcontacto" } });
            }
            else
            {
                l.Insert(10, new CampoDef { Key = "TipoCliente", Label = "Tipo de cliente", Alias = new[] { "tipocliente", "tipo", "type", "categoriacliente" } });
            }
            return l;
        }

        /// <summary>Detecta columnas y corrige casos típicos (p. ej. el nombre viene en «descripción» o «razón social»).</summary>
        public static Dictionary<string, string> Detectar(List<string> headers, string key)
        {
            if (MigracionOperacionesHelper.EsCatalogo(key)) return MigracionOperacionesHelper.Detectar(headers, key);
            var campos = Campos(key);
            var mapa = MigracionHistorialHelper.Detectar(headers, campos)
                .Where(p => p.Value != null).ToDictionary(p => p.Key, p => p.Value!);

            if (!mapa.ContainsKey("Nombre"))
            {
                foreach (var alterno in new[] { "RazonSocial", "Descripcion" })
                {
                    if (mapa.TryGetValue(alterno, out var col))
                    {
                        mapa["Nombre"] = col;
                        mapa.Remove(alterno);
                        break;
                    }
                }
            }
            return mapa;
        }

        public static List<string> Faltantes(string key, Dictionary<string, string> mapa) =>
            MigracionOperacionesHelper.EsCatalogo(key) ? MigracionOperacionesHelper.Faltantes(key, mapa)
            : Campos(key).Where(c => c.Requerido && !mapa.ContainsKey(c.Key)).Select(c => c.Label).ToList();

        // ================= Procesar =================
        public static Resultado Procesar(ApplicationDbContext ctx, TablaOrigen tabla, string key,
            Dictionary<string, string> mapa, int? usuarioId, bool tolerante = true)
        {
            var r = new Resultado { Tipo = key, Filas = tabla.Rows.Count, Tolerante = tolerante };
            var falta = Faltantes(key, mapa);
            if (falta.Count > 0)
            {
                r.Errores.Add("No se encontró la columna de: " + string.Join(", ", falta) + ".");
                return r;
            }

            switch (key)
            {
                case "Impuestos": Impuestos(ctx, tabla, mapa, r); break;
                case "UnidadesMedida": Unidades(ctx, tabla, mapa, r); break;
                case "Categorias": Categorias(ctx, tabla, mapa, r); break;
                case "Productos": Productos(ctx, tabla, mapa, r, usuarioId); break;
                case "Clientes": Terceros(ctx, tabla, mapa, r, usuarioId, false); break;
                case "Proveedores": Terceros(ctx, tabla, mapa, r, usuarioId, true); break;
                default:
                    if (MigracionOperacionesHelper.EsCatalogo(key)) MigracionOperacionesHelper.ProcesarCatalogo(ctx, tabla, key, mapa, r, usuarioId);
                    else r.Errores.Add("Tipo de catálogo desconocido.");
                    break;
            }
            r.Aplicado = r.Errores.Count == 0;
            return r;
        }

        private static string? V(Dictionary<string, string> fila, Dictionary<string, string> mapa, string campo) =>
            MigracionHistorialHelper.Valor(fila, mapa, campo);

        private static bool Activo(Dictionary<string, string> fila, Dictionary<string, string> mapa, string campo = "Activo")
        {
            var t = V(fila, mapa, campo);
            if (t == null) return true;
            var b = ParserHelper.ParsearBool(t);
            if (b != null) return b.Value;
            var n = MigracionHistorialHelper.Norm(t);
            return !(n.Contains("inactiv") || n.Contains("baja") || n.Contains("deshab") || n == "i" || n == "0");
        }

        // ---------- Impuestos ----------
        private static void Impuestos(ApplicationDbContext ctx, TablaOrigen tabla, Dictionary<string, string> mapa, Resultado r)
        {
            var existentes = ctx.Impuestos.AsNoTracking().ToList();
            var nombres = existentes.Select(i => MigracionHistorialHelper.Norm(i.Nombre)).ToHashSet();
            var porcentajes = existentes.Select(i => i.Porcentaje).ToHashSet();
            int n = 0;
            foreach (var fila in tabla.Rows)
            {
                MigracionCorrida.Fila(tabla.Rows.Count);
                n++; int nf = n + 1;
                var pctTxt = V(fila, mapa, "Porcentaje");
                var pct = ParserHelper.ParsearDecimal(pctTxt);
                if (pct == null || pct < 0) { r.Omitidos++; r.Error(nf, "porcentaje inválido."); continue; }
                if (pct > 0 && pct < 1) { pct *= 100; r.Aviso(nf, $"porcentaje {pctTxt} interpretado como {pct}%."); }
                if (pct > 100) { r.Omitidos++; r.Error(nf, "porcentaje mayor a 100."); continue; }

                var nombre = V(fila, mapa, "Nombre") ?? $"Impuesto {pct:0.##}%";
                var nn = MigracionHistorialHelper.Norm(nombre);
                if (nombres.Contains(nn) || porcentajes.Contains(pct.Value)) { r.YaImportados++; continue; }
                nombres.Add(nn); porcentajes.Add(pct.Value);

                ctx.Impuestos.Add(new Impuesto
                {
                    Nombre = Recortar(nombre, 50), Porcentaje = pct.Value,
                    Descripcion = V(fila, mapa, "Descripcion") is string d ? Recortar(d, 200) : null,
                    Activo = Activo(fila, mapa), EsPredeterminado = false
                });
                r.Creados++;
            }
            ctx.SaveChanges();
        }

        // ---------- Unidades ----------
        private static string AbreviaturaDe(string nombre)
        {
            var letras = new string(nombre.Where(char.IsLetterOrDigit).ToArray());
            if (letras.Length == 0) return "U";
            return Recortar(letras.ToUpperInvariant(), letras.Length <= 4 ? letras.Length : 3);
        }

        private static void Unidades(ApplicationDbContext ctx, TablaOrigen tabla, Dictionary<string, string> mapa, Resultado r)
        {
            var existentes = ctx.UnidadesMedida.AsNoTracking().ToList();
            var claves = new HashSet<string>();
            foreach (var u in existentes) { claves.Add(MigracionHistorialHelper.Norm(u.Nombre)); claves.Add(MigracionHistorialHelper.Norm(u.Abreviatura)); }
            int n = 0;
            foreach (var fila in tabla.Rows)
            {
                MigracionCorrida.Fila(tabla.Rows.Count);
                n++; int nf = n + 1;
                var nombre = V(fila, mapa, "Nombre");
                if (nombre == null) { r.Omitidos++; r.Error(nf, "sin nombre."); continue; }
                var abrev = V(fila, mapa, "Abreviatura") ?? AbreviaturaDe(nombre);
                var kn = MigracionHistorialHelper.Norm(nombre); var ka = MigracionHistorialHelper.Norm(abrev);
                if (claves.Contains(kn) || claves.Contains(ka)) { r.YaImportados++; continue; }
                claves.Add(kn); claves.Add(ka);
                ctx.UnidadesMedida.Add(new UnidadMedida { Nombre = Recortar(nombre, 50), Abreviatura = Recortar(abrev, 10), Activa = Activo(fila, mapa, "Activa") });
                r.Creados++;
            }
            ctx.SaveChanges();
        }

        // ---------- Categorías ----------
        private static readonly Regex ColorHex = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

        private static void Categorias(ApplicationDbContext ctx, TablaOrigen tabla, Dictionary<string, string> mapa, Resultado r)
        {
            var nombres = ctx.Categorias.AsNoTracking().Select(c => c.Nombre).ToList()
                .Select(MigracionHistorialHelper.Norm).ToHashSet();
            int n = 0;
            foreach (var fila in tabla.Rows)
            {
                MigracionCorrida.Fila(tabla.Rows.Count);
                n++; int nf = n + 1;
                var nombre = V(fila, mapa, "Nombre");
                if (nombre == null) { r.Omitidos++; r.Error(nf, "sin nombre."); continue; }
                if (!nombres.Add(MigracionHistorialHelper.Norm(nombre))) { r.YaImportados++; continue; }
                var color = V(fila, mapa, "Color");
                var c = new Categoria
                {
                    Nombre = Recortar(nombre, 100),
                    Descripcion = V(fila, mapa, "Descripcion") is string d ? Recortar(d, 300) : null,
                    Activa = Activo(fila, mapa, "Activa")
                };
                if (color != null && ColorHex.IsMatch(color)) c.Color = color;
                ctx.Categorias.Add(c);
                r.Creados++;
            }
            ctx.SaveChanges();
        }

        // ---------- Productos ----------
        private static void Productos(ApplicationDbContext ctx, TablaOrigen tabla, Dictionary<string, string> mapa, Resultado r, int? usuarioId)
        {
            if (!mapa.ContainsKey("PrecioVenta"))
                r.Aviso(0, "No se encontró la columna del precio de venta: los productos entran con precio 0 (corrígelos después).");

            var categorias = ctx.Categorias.AsNoTracking().ToList();
            var impuestos = ctx.Impuestos.AsNoTracking().ToList();
            var unidades = ctx.UnidadesMedida.AsNoTracking().ToList();

            var skus = ctx.Productos.AsNoTracking().Where(p => p.SKU != null).Select(p => p.SKU!).ToList()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var barras = ctx.Productos.AsNoTracking().Where(p => p.CodigoBarras != null).Select(p => p.CodigoBarras!).ToList()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var nombres = ctx.Productos.AsNoTracking().Select(p => p.Nombre).ToList()
                .Select(MigracionHistorialHelper.Norm).ToHashSet();

            var serie = ctx.SeriesDocumentos.FirstOrDefault(s => s.Tipo == "Producto" && s.EsPredeterminada && s.Activa);
            string prefijo = serie?.Prefijo ?? "PRO", sep = serie?.Separador ?? "-";
            int largo = serie?.LongitudNumero ?? 4;
            int contador = serie?.SiguienteNumero ?? (skus.Count + 1);
            string NuevoSku()
            {
                string s;
                do { s = prefijo + sep + contador.ToString().PadLeft(largo, '0'); contador++; } while (skus.Contains(s));
                return s;
            }

            int ResolverCategoria(string? valor)
            {
                if (string.IsNullOrWhiteSpace(valor)) return 0;
                var kn = MigracionHistorialHelper.Norm(valor);
                var c = categorias.FirstOrDefault(x => MigracionHistorialHelper.Norm(x.Nombre) == kn);
                if (c != null) return c.Id;
                c = new Categoria { Nombre = Recortar(valor, 100), Activa = true };
                ctx.Categorias.Add(c); ctx.SaveChanges();
                categorias.Add(c);
                return c.Id;
            }

            int ResolverImpuesto(string? valor)
            {
                if (string.IsNullOrWhiteSpace(valor)) return 0;
                var kn = MigracionHistorialHelper.Norm(valor);
                var i = impuestos.FirstOrDefault(x => MigracionHistorialHelper.Norm(x.Nombre) == kn);
                if (i != null) return i.Id;
                var pct = ParserHelper.ParsearDecimal(valor);
                if (pct == null || pct < 0 || pct > 100) return 0;     // «Exento», «N/A»… → sin impuesto
                i = impuestos.FirstOrDefault(x => x.Porcentaje == pct.Value);
                if (i != null) return i.Id;
                i = new Impuesto { Nombre = Recortar(valor, 50), Porcentaje = pct.Value, Activo = true };
                ctx.Impuestos.Add(i); ctx.SaveChanges();
                impuestos.Add(i);
                return i.Id;
            }

            string ResolverUnidad(string? valor)
            {
                if (string.IsNullOrWhiteSpace(valor)) return "Unidad";
                var kn = MigracionHistorialHelper.Norm(valor);
                var u = unidades.FirstOrDefault(x => MigracionHistorialHelper.Norm(x.Nombre) == kn)
                        ?? unidades.FirstOrDefault(x => MigracionHistorialHelper.Norm(x.Abreviatura) == kn);
                if (u == null)
                {
                    u = new UnidadMedida { Nombre = Recortar(valor, 20), Abreviatura = Recortar(AbreviaturaDe(valor), 10), Activa = true };
                    ctx.UnidadesMedida.Add(u); ctx.SaveChanges();
                    unidades.Add(u);
                }
                return Recortar(u.Nombre, 20);
            }

            var nuevos = new List<(Producto p, decimal stock)>();
            int n = 0;
            foreach (var fila in tabla.Rows)
            {
                MigracionCorrida.Fila(tabla.Rows.Count);
                n++; int nf = n + 1;
                var nombre = V(fila, mapa, "Nombre");
                if (nombre == null) { r.Omitidos++; r.Error(nf, "producto sin nombre."); continue; }
                nombre = Recortar(nombre, 150);

                var sku = V(fila, mapa, "SKU");
                var cb = V(fila, mapa, "CodigoBarras");
                if (sku != null && sku.Length > 50) { r.Aviso(nf, "SKU muy largo: se generó uno nuevo."); sku = null; }
                if (cb != null && cb.Length > 50) { r.Aviso(nf, "código de barras muy largo: se ignoró."); cb = null; }

                bool yaExiste = (sku != null && skus.Contains(sku)) || (cb != null && barras.Contains(cb))
                                || (sku == null && cb == null && nombres.Contains(MigracionHistorialHelper.Norm(nombre)));
                if (yaExiste) { r.YaImportados++; continue; }

                decimal Num(string campo, decimal def = 0)
                {
                    var t = V(fila, mapa, campo);
                    if (t == null) return def;
                    var d = ParserHelper.ParsearDecimal(t);
                    if (d == null) { r.Aviso(nf, $"«{t}» no es un número válido en {campo}; se usó {def}."); return def; }
                    if (d < 0 && campo != "Stock") { r.Aviso(nf, $"{campo} negativo; se usó 0."); return 0; }
                    return d.Value;
                }

                var stock = Num("Stock");
                if (stock < 0) { r.Aviso(nf, $"«{nombre}» tiene existencia negativa ({stock:0.##}); entra con stock 0."); stock = 0; }

                if (sku == null) sku = NuevoSku();
                skus.Add(sku);
                if (cb != null) barras.Add(cb);
                nombres.Add(MigracionHistorialHelper.Norm(nombre));

                var catId = ResolverCategoria(V(fila, mapa, "Categoria"));
                var impId = ResolverImpuesto(V(fila, mapa, "Impuesto"));

                var prod = new Producto
                {
                    SKU = sku, CodigoBarras = cb, Nombre = nombre,
                    Descripcion = V(fila, mapa, "Descripcion"),
                    CategoriaId = catId == 0 ? null : catId,
                    ImpuestoId = impId == 0 ? null : impId,
                    PrecioCompra = Num("PrecioCompra"), PrecioVenta = Num("PrecioVenta"), PrecioMayorista = Num("PrecioMayorista"),
                    Stock = 0, StockMinimo = Num("StockMinimo"),
                    UnidadMedida = ResolverUnidad(V(fila, mapa, "UnidadMedida")),
                    Activo = Activo(fila, mapa), FechaCreacion = DateTime.Now
                };
                ctx.Productos.Add(prod);
                if (stock > 0) nuevos.Add((prod, stock));
                r.Creados++;
                r.Monto += stock * prod.PrecioCompra;     // valor del inventario inicial al costo
            }
            ctx.SaveChanges();

            foreach (var (prod, cant) in nuevos)
                InventarioHelper.Registrar(ctx, prod, cant, "StockInicial", null, prod.Id, usuarioId, prod.PrecioCompra,
                    "Stock inicial por migración");
            if (nuevos.Count > 0) ctx.SaveChanges();

            if (serie != null && r.Creados > 0)
            {
                serie.SiguienteNumero = contador;
                ctx.SaveChanges();
            }
        }

        // ---------- Clientes / Proveedores ----------
        private static void Terceros(ApplicationDbContext ctx, TablaOrigen tabla, Dictionary<string, string> mapa, Resultado r,
            int? usuarioId, bool proveedor)
        {
            var indice = proveedor ? IndiceProveedores(ctx) : IndiceClientes(ctx);
            var vistos = new HashSet<string>();
            int n = 0;
            foreach (var fila in tabla.Rows)
            {
                MigracionCorrida.Fila(tabla.Rows.Count);
                n++; int nf = n + 1;
                var nombre = V(fila, mapa, "Nombre") ?? V(fila, mapa, "RazonSocial");
                if (nombre == null) { r.Omitidos++; r.Error(nf, "sin nombre."); continue; }
                var codigo = V(fila, mapa, "Codigo");
                var rtn = V(fila, mapa, "RTN");

                if (indice.Buscar(codigo, rtn, nombre) != null) { r.YaImportados++; continue; }
                var clave = ClaveTercero(codigo, rtn, nombre);
                if (!vistos.Add(clave)) { r.YaImportados++; r.Aviso(nf, $"«{nombre}» está repetido en el archivo; se tomó la primera fila."); continue; }

                if (codigo != null && (codigo.Length > 20 || indice.CodigosUsados.Contains(codigo)))
                { r.Aviso(nf, $"el código «{codigo}» no se pudo usar (repetido o muy largo); queda sin código."); codigo = null; }
                if (codigo != null) indice.CodigosUsados.Add(codigo);

                var email = V(fila, mapa, "Email");
                if (email != null && (!email.Contains('@') || email.Length > 150)) { r.Aviso(nf, $"correo «{email}» inválido; se ignoró."); email = null; }

                var dias = (int?)ParserHelper.ParsearDecimal(V(fila, mapa, "DiasCredito")) ?? 0;
                if (dias < 0) dias = 0;
                var limite = ParserHelper.ParsearDecimal(V(fila, mapa, "LimiteCredito")) ?? 0;
                if (limite < 0) limite = 0;
                var activo = Activo(fila, mapa);

                string? R(string campo, int max) => V(fila, mapa, campo) is string s ? Recortar(s, max) : null;

                if (proveedor)
                {
                    ctx.Proveedores.Add(new Proveedor
                    {
                        Codigo = codigo, Nombre = Recortar(nombre, 150), RazonSocial = R("RazonSocial", 200),
                        RTN = rtn == null ? null : Recortar(rtn, 20), Email = email, Telefono = R("Telefono", 30),
                        Contacto = R("Contacto", 100), TelefonoContacto = R("TelefonoContacto", 30),
                        Direccion = R("Direccion", 300), Ciudad = R("Ciudad", 100),
                        DiasCredito = dias, LimiteCredito = limite, CondicionPago = dias > 0 ? "Credito" : "Contado",
                        Notas = V(fila, mapa, "Notas"), Activo = activo, UsuarioCreoId = usuarioId
                    });
                }
                else
                {
                    ctx.Clientes.Add(new Cliente
                    {
                        Codigo = codigo, Nombre = Recortar(nombre, 150), RazonSocial = R("RazonSocial", 200),
                        RTN = rtn == null ? null : Recortar(rtn, 20), Email = email, Telefono = R("Telefono", 30),
                        Direccion = R("Direccion", 300), Ciudad = R("Ciudad", 100),
                        TipoCliente = R("TipoCliente", 20) ?? "Regular",
                        DiasCredito = dias, LimiteCredito = limite,
                        Notas = V(fila, mapa, "Notas"), Activo = activo, UsuarioCreoId = usuarioId
                    });
                }
                r.Creados++;
            }
            ctx.SaveChanges();
        }
    }
}
