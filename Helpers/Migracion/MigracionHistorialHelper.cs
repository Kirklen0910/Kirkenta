using System.Globalization;
using Kirkenta.Data;
using Kirkenta.Helpers.Import;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers.Migracion
{
    /// <summary>
    /// Etapa 2 de la migración: saldos por cobrar, saldos por pagar, historial de ventas y kardex histórico.
    /// Reglas de oro:
    ///  - NADA de esto mueve el stock actual (el stock inicial entra por Productos) ni la caja/bancos.
    ///  - Los saldos por cobrar/pagar se crean como una venta a crédito / orden recibida, de modo que se
    ///    cobran en Finanzas → Cobros y se pagan en Pagos a proveedores como cualquier otro (ahí SÍ entra el dinero).
    ///  - Todo queda marcado con un lote para poder deshacerlo.
    ///  - Volver a importar el mismo archivo no duplica (se detectan los números ya importados).
    /// </summary>
    public static class MigracionHistorialHelper
    {
        public const string TipoCxC = "cxc";
        public const string TipoCxP = "cxp";
        public const string TipoVentas = "ventas";
        public const string TipoKardex = "kardex";

        public const string OrigenKardex = "Migracion";
        public const string GenericoSku = "MIG-GENERICO";

        public static readonly string[] Tipos =
            new[] { TipoCxC, TipoCxP, TipoVentas, TipoKardex }.Concat(MigracionOperacionesHelper.Historiales).ToArray();

        public static string NombreTipo(string tipo) => tipo switch
        {
            TipoCxC => "Saldos por cobrar (clientes)",
            TipoCxP => "Saldos por pagar (proveedores)",
            TipoVentas => "Historial de ventas / facturas",
            TipoKardex => "Movimientos de inventario (kardex histórico)",
            _ => MigracionOperacionesHelper.Nombre(tipo)
        };

        // ================= Definición de campos =================
        public class CampoDef
        {
            public string Key { get; init; } = "";
            public string Label { get; init; } = "";
            public bool Requerido { get; init; }
            public string[] Alias { get; init; } = Array.Empty<string>();
        }

        public static List<CampoDef> Campos(string tipo) => tipo switch
        {
            TipoCxC => new()
            {
                new() { Key = "Documento", Label = "N.º de factura / documento", Requerido = true, Alias = new[] { "documento", "factura", "numero", "nodocumento", "numerofactura", "referencia", "folio", "doc" } },
                new() { Key = "Saldo", Label = "Saldo pendiente", Requerido = true, Alias = new[] { "saldo", "saldopendiente", "pendiente", "balance", "porcobrar", "adeudado", "debe" } },
                new() { Key = "Cliente", Label = "Cliente (nombre)", Alias = new[] { "cliente", "nombrecliente", "nombre", "razonsocial", "customer" } },
                new() { Key = "ClienteCodigo", Label = "Código del cliente", Alias = new[] { "codigocliente", "idcliente", "clientecodigo", "codcliente" } },
                new() { Key = "ClienteRTN", Label = "RTN del cliente", Alias = new[] { "rtn", "rtncliente", "ruc", "nit", "identidad" } },
                new() { Key = "Fecha", Label = "Fecha de la factura", Alias = new[] { "fecha", "fechafactura", "fechaemision", "emision", "date" } },
                new() { Key = "Vencimiento", Label = "Fecha de vencimiento", Alias = new[] { "vencimiento", "fechavencimiento", "vence", "fechavence", "duedate" } },
                new() { Key = "Total", Label = "Total original (opcional)", Alias = new[] { "total", "montooriginal", "totalfactura", "monto", "importe" } },
                new() { Key = "Notas", Label = "Notas (opcional)", Alias = new[] { "notas", "observaciones", "comentario", "descripcion" } },
            },
            TipoCxP => new()
            {
                new() { Key = "Documento", Label = "N.º de factura / documento", Requerido = true, Alias = new[] { "documento", "factura", "numero", "nodocumento", "numerofactura", "referencia", "folio", "doc" } },
                new() { Key = "Saldo", Label = "Saldo pendiente", Requerido = true, Alias = new[] { "saldo", "saldopendiente", "pendiente", "balance", "porpagar", "adeudado", "debemos" } },
                new() { Key = "Proveedor", Label = "Proveedor (nombre)", Alias = new[] { "proveedor", "nombreproveedor", "nombre", "razonsocial", "supplier", "vendor" } },
                new() { Key = "ProveedorCodigo", Label = "Código del proveedor", Alias = new[] { "codigoproveedor", "idproveedor", "proveedorcodigo", "codproveedor" } },
                new() { Key = "ProveedorRTN", Label = "RTN del proveedor", Alias = new[] { "rtn", "rtnproveedor", "ruc", "nit" } },
                new() { Key = "Fecha", Label = "Fecha de la factura", Alias = new[] { "fecha", "fechafactura", "fechaemision", "emision", "date" } },
                new() { Key = "Vencimiento", Label = "Fecha de vencimiento", Alias = new[] { "vencimiento", "fechavencimiento", "vence", "fechavence", "duedate" } },
                new() { Key = "Total", Label = "Total original (opcional)", Alias = new[] { "total", "montooriginal", "totalfactura", "monto", "importe" } },
                new() { Key = "Notas", Label = "Notas (opcional)", Alias = new[] { "notas", "observaciones", "comentario", "descripcion" } },
            },
            TipoVentas => new()
            {
                new() { Key = "Documento", Label = "N.º de factura / venta", Requerido = true, Alias = new[] { "documento", "factura", "numero", "nodocumento", "numerofactura", "folio", "ticket", "venta", "idventa", "doc" } },
                new() { Key = "Fecha", Label = "Fecha", Requerido = true, Alias = new[] { "fecha", "fechafactura", "fechaventa", "emision", "date" } },
                new() { Key = "Total", Label = "Total", Requerido = true, Alias = new[] { "total", "totalfactura", "importe", "montototal", "grandtotal" } },
                new() { Key = "Subtotal", Label = "Subtotal (opcional)", Alias = new[] { "subtotal", "sumas", "neto", "importeneto" } },
                new() { Key = "Descuento", Label = "Descuento (opcional)", Alias = new[] { "descuento", "discount", "rebaja" } },
                new() { Key = "Impuestos", Label = "ISV / impuestos (opcional)", Alias = new[] { "impuestos", "impuesto", "isv", "iva", "tax" } },
                new() { Key = "Cliente", Label = "Cliente (nombre)", Alias = new[] { "cliente", "nombrecliente", "razonsocial", "customer" } },
                new() { Key = "ClienteCodigo", Label = "Código del cliente", Alias = new[] { "codigocliente", "idcliente", "clientecodigo", "codcliente" } },
                new() { Key = "ClienteRTN", Label = "RTN del cliente", Alias = new[] { "rtn", "rtncliente", "ruc", "nit" } },
                new() { Key = "Saldo", Label = "Saldo pendiente (si fue a crédito; opcional)", Alias = new[] { "saldo", "saldopendiente", "pendiente", "porcobrar", "balance" } },
                new() { Key = "Vencimiento", Label = "Fecha de vencimiento (opcional)", Alias = new[] { "vencimiento", "fechavencimiento", "vence", "duedate" } },
                new() { Key = "Estado", Label = "Estado (las anuladas se omiten)", Alias = new[] { "estado", "status", "anulada", "anulado" } },
                new() { Key = "FormaPago", Label = "Forma de pago (opcional)", Alias = new[] { "formapago", "pago", "metodopago", "tipopago", "condicion" } },
                new() { Key = "Notas", Label = "Notas (opcional)", Alias = new[] { "notas", "observaciones", "comentario" } },
            },
            TipoKardex => new()
            {
                new() { Key = "ProductoCodigo", Label = "Código / SKU del producto", Alias = new[] { "sku", "codigo", "codigoproducto", "codigoarticulo", "code", "cod", "barras" } },
                new() { Key = "ProductoNombre", Label = "Nombre del producto", Alias = new[] { "producto", "nombreproducto", "articulo", "descripcion", "nombre" } },
                new() { Key = "Fecha", Label = "Fecha", Requerido = true, Alias = new[] { "fecha", "date", "fechamovimiento" } },
                new() { Key = "Cantidad", Label = "Cantidad", Requerido = true, Alias = new[] { "cantidad", "qty", "unidades" } },
                new() { Key = "Tipo", Label = "Tipo (entrada/salida; opcional si la cantidad trae signo)", Alias = new[] { "tipo", "tipomovimiento", "movimiento", "operacion", "concepto" } },
                new() { Key = "Costo", Label = "Costo unitario (opcional)", Alias = new[] { "costo", "costounitario", "cost", "preciocosto" } },
                new() { Key = "Documento", Label = "Documento (opcional)", Alias = new[] { "documento", "referencia", "factura", "numero", "doc" } },
                new() { Key = "StockResultante", Label = "Existencia resultante (opcional)", Alias = new[] { "existencia", "saldo", "stock", "balance", "stockposterior" } },
                new() { Key = "Notas", Label = "Notas (opcional)", Alias = new[] { "notas", "observaciones", "comentario" } },
            },
            _ => MigracionOperacionesHelper.CamposHistorial(tipo)
        };

        /// <summary>Líneas de las ventas (solo para el historial de ventas; tabla aparte).</summary>
        public static List<CampoDef> CamposDetalle() => new()
        {
            new() { Key = "Documento", Label = "N.º de documento (para unirla)", Requerido = true, Alias = new[] { "documento", "factura", "numero", "nodocumento", "numerofactura", "folio", "ticket", "venta", "idventa", "doc", "compra", "idcompra", "ordencompra", "cotizacion", "idcotizacion", "presupuesto" } },
            new() { Key = "ProductoCodigo", Label = "Código / SKU del producto", Alias = new[] { "sku", "codigo", "codigoproducto", "codigoarticulo", "code", "cod", "barras" } },
            new() { Key = "ProductoNombre", Label = "Nombre del producto", Alias = new[] { "producto", "nombreproducto", "articulo", "descripcion", "nombre", "detalle" } },
            new() { Key = "Cantidad", Label = "Cantidad", Requerido = true, Alias = new[] { "cantidad", "qty", "unidades" } },
            new() { Key = "Precio", Label = "Precio unitario", Requerido = true, Alias = new[] { "precio", "preciounitario", "price", "pvp", "valorunitario" } },
            new() { Key = "Descuento", Label = "Descuento de la línea (opcional)", Alias = new[] { "descuento", "discount" } },
            new() { Key = "TotalLinea", Label = "Total de la línea (opcional)", Alias = new[] { "totallinea", "importe", "total", "subtotal" } },
            new() { Key = "Costo", Label = "Costo unitario (opcional)", Alias = new[] { "costo", "costounitario", "cost" } },
        };

        public static Dictionary<string, string?> Detectar(List<string> headers, List<CampoDef> campos) =>
            ColumnMapper.Detectar(headers, campos.ToDictionary(c => c.Key, c => c.Alias));

        /// <summary>Busca entre las demás tablas la que parece contener las líneas del documento (ventas, compras o cotizaciones).</summary>
        public static int? SugerirTablaDetalle(List<TablaOrigen> tablas, int indicePrincipal, string tipo = TipoVentas)
        {
            // Pistas del nombre según el documento: «detallecompras» va con compras, no con ventas
            var pistas = tipo switch
            {
                MigracionOperacionesHelper.TipoCompras => new[] { "compra", "purchase", "orden" },
                MigracionOperacionesHelper.TipoCotizaciones => new[] { "cotiz", "presup", "proforma", "quote" },
                _ => new[] { "venta", "factura", "ticket", "sale", "invoice" }
            };
            var otras = new[] { "compra", "purchase", "cotiz", "presup", "venta", "factura", "ticket", "sale", "invoice" }.Except(pistas).ToArray();
            var principal = ColumnMapper.Normalizar(tablas[indicePrincipal].Nombre);
            var campos = CamposDetalle();
            int? mejor = null; int mejorPuntos = 0;
            for (int i = 0; i < tablas.Count; i++)
            {
                if (i == indicePrincipal) continue;
                var mapa = Detectar(tablas[i].Headers, campos);
                bool doc = mapa["Documento"] != null, cant = mapa["Cantidad"] != null, precio = mapa["Precio"] != null;
                if (!(doc && cant && precio)) continue;
                var nombre = ColumnMapper.Normalizar(tablas[i].Nombre);
                if (otras.Any(nombre.Contains) && !pistas.Any(nombre.Contains)) continue;   // líneas de otro tipo de documento
                int puntos = 3 + (nombre.Contains("detalle") || nombre.Contains("linea") || nombre.Contains("item") ? 3 : 0)
                               + (mapa["ProductoCodigo"] != null || mapa["ProductoNombre"] != null ? 2 : 0)
                               + (pistas.Any(nombre.Contains) || (principal.Length > 3 && nombre.Contains(principal)) ? 4 : 0);
                if (puntos > mejorPuntos) { mejorPuntos = puntos; mejor = i; }
            }
            return mejor;
        }

        // ================= Resultado =================
        public class Opciones
        {
            public string Tipo { get; set; } = TipoCxC;
            public Dictionary<string, string> Mapa { get; set; } = new();
            public TablaOrigen? Detalle { get; set; }
            public Dictionary<string, string> MapaDetalle { get; set; } = new();
            /// <summary>"crear" = crear el cliente/proveedor que no exista; "sin" = ventas a consumidor final (solo ventas).</summary>
            public string TerceroNoExiste { get; set; } = "crear";
            /// <summary>Migración automática: una fila mala se descarta (y se informa) en vez de bloquear todo el paso.</summary>
            public bool Tolerante { get; set; }
        }

        public class Resultado
        {
            public string Tipo { get; set; } = "";
            public bool Aplicado { get; set; }
            public string? Lote { get; set; }
            public int Filas { get; set; }
            public int Creados { get; set; }
            public int YaImportados { get; set; }
            public int Omitidos { get; set; }
            public int TercerosPorCrear { get; set; }
            public int TercerosCreados { get; set; }
            public int LineasCreadas { get; set; }
            public decimal Monto { get; set; }
            public bool Tolerante { get; set; }
            public int DescartadasTotal { get; set; }
            public List<string> Descartes { get; } = new();
            public List<string> Errores { get; } = new();
            public List<string> Advertencias { get; } = new();
            public bool Ok => Errores.Count == 0;

            internal void Descarte(int fila, string msg)
            {
                DescartadasTotal++;
                if (Descartes.Count < 300) Descartes.Add($"Fila {fila}: {msg}");
                else if (Descartes.Count == 300) Descartes.Add("… y más filas descartadas (se muestran las primeras 300).");
            }

            internal void Error(int fila, string msg) { if (Tolerante) { Descarte(fila, msg); return; } if (Errores.Count < 300) Errores.Add($"Fila {fila}: {msg}"); else if (Errores.Count == 300) Errores.Add("… y más errores (se muestran los primeros 300)."); }
            internal void Aviso(int fila, string msg) { if (Advertencias.Count < 100) Advertencias.Add(fila > 0 ? $"Fila {fila}: {msg}" : msg); }
        }

        // ================= Utilidades =================
        internal static string Norm(string? s) => ColumnMapper.Normalizar(s);
        internal static string SoloDigitos(string? s) => new((s ?? "").Where(char.IsDigit).ToArray());

        internal static string? Valor(Dictionary<string, string> fila, Dictionary<string, string> mapa, string campo)
        {
            if (!mapa.TryGetValue(campo, out var col) || string.IsNullOrEmpty(col)) return null;
            if (!fila.TryGetValue(col, out var v)) return null;
            v = v?.Trim();
            return string.IsNullOrEmpty(v) ? null : v;
        }

        private static readonly string[] FormatosFecha =
        {
            "dd/MM/yyyy", "d/M/yyyy", "dd/MM/yyyy HH:mm:ss", "d/M/yyyy H:mm:ss", "d/M/yyyy H:mm", "dd/MM/yyyy H:mm:ss",
            "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ss.fff", "yyyy-MM-ddTHH:mm:ssZ",
            "dd-MM-yyyy", "d-M-yyyy", "yyyy/MM/dd", "dd/MM/yy", "d/M/yy", "yyyyMMdd"
        };

        public static DateTime? ParsearFecha(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();

            // Número de serie de Excel
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var serie) && serie > 20000 && serie < 80000 && !s.Contains('/') )
            {
                try { return DateTime.FromOADate(serie); } catch { /* sigue */ }
            }

            if (DateTime.TryParseExact(s, FormatosFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out var f)) return f;
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out f)) return f;
            return null;
        }

        private static bool FechaPlausible(DateTime f) => f.Year >= 1990 && f <= DateTime.Today.AddYears(1);

        internal static string Recortar(string s, int max) => s.Length <= max ? s : s.Substring(0, max);

        public static string NuevoLote() => "MIG-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");

        // ---------- Terceros (clientes / proveedores) ----------
        internal class Tercero
        {
            public int Id; public string? Codigo; public string Rtn = ""; public string Nombre = ""; public string Razon = "";
        }

        internal class IndiceTerceros
        {
            public Dictionary<string, int> PorCodigo = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> PorRtn = new();
            public Dictionary<string, int> PorNombre = new();
            public HashSet<string> CodigosUsados = new(StringComparer.OrdinalIgnoreCase);

            public void Agregar(Tercero t)
            {
                if (!string.IsNullOrWhiteSpace(t.Codigo)) { PorCodigo.TryAdd(t.Codigo!.Trim(), t.Id); CodigosUsados.Add(t.Codigo!.Trim()); }
                if (t.Rtn.Length >= 6) PorRtn.TryAdd(t.Rtn, t.Id);
                var n = Norm(t.Nombre); if (n.Length > 0) PorNombre.TryAdd(n, t.Id);
                var r = Norm(t.Razon); if (r.Length > 0) PorNombre.TryAdd(r, t.Id);
            }

            public int? Buscar(string? codigo, string? rtn, string? nombre)
            {
                if (!string.IsNullOrWhiteSpace(codigo) && PorCodigo.TryGetValue(codigo.Trim(), out var a)) return a;
                var d = SoloDigitos(rtn);
                if (d.Length >= 6 && PorRtn.TryGetValue(d, out var b)) return b;
                var n = Norm(nombre);
                if (n.Length > 0 && PorNombre.TryGetValue(n, out var c)) return c;
                return null;
            }
        }

        internal static string ClaveTercero(string? codigo, string? rtn, string? nombre)
        {
            var d = SoloDigitos(rtn);
            if (d.Length >= 6) return "R" + d;
            var n = Norm(nombre);
            if (n.Length > 0) return "N" + n;
            return "C" + (codigo ?? "").Trim().ToLowerInvariant();
        }

        internal static IndiceTerceros IndiceClientes(ApplicationDbContext ctx)
        {
            var idx = new IndiceTerceros();
            foreach (var c in ctx.Clientes.AsNoTracking().Select(c => new { c.Id, c.Codigo, c.RTN, c.Nombre, c.RazonSocial }).ToList())
                idx.Agregar(new Tercero { Id = c.Id, Codigo = c.Codigo, Rtn = SoloDigitos(c.RTN), Nombre = c.Nombre, Razon = c.RazonSocial ?? "" });
            return idx;
        }

        internal static IndiceTerceros IndiceProveedores(ApplicationDbContext ctx)
        {
            var idx = new IndiceTerceros();
            foreach (var c in ctx.Proveedores.AsNoTracking().Select(c => new { c.Id, c.Codigo, c.RTN, c.Nombre, c.RazonSocial }).ToList())
                idx.Agregar(new Tercero { Id = c.Id, Codigo = c.Codigo, Rtn = SoloDigitos(c.RTN), Nombre = c.Nombre, Razon = c.RazonSocial ?? "" });
            return idx;
        }

        // ---------- Plan intermedio ----------
        private class LineaPlan
        {
            public string? Codigo, Nombre; public decimal Cantidad, Precio, Descuento, Total, Costo;
        }

        private class FilaPlan
        {
            public int Fila; public string Documento = ""; public DateTime Fecha; public DateTime? Vence;
            public decimal Total, Subtotal, Descuento, Impuestos, Saldo; public string? Notas;
            public string? TerceroCodigo, TerceroRtn, TerceroNombre; public string ClaveTercero = "";
            public int? TerceroId; public string Numero = "";
            public List<LineaPlan> Lineas = new();
        }

        // ================= Punto de entrada =================
        /// <summary>
        /// Procesa la tabla. Con aplicar=false solo valida y cuenta (vista previa); con aplicar=true guarda todo en
        /// una transacción (si algo falla no queda nada a medias).
        /// </summary>
        public static Resultado Procesar(ApplicationDbContext ctx, TablaOrigen tabla, Opciones op, bool aplicar, int? usuarioId)
        {
            if (op.Tipo == TipoKardex) return ProcesarKardex(ctx, tabla, op, aplicar, usuarioId);
            if (MigracionOperacionesHelper.EsHistorial(op.Tipo)) return MigracionOperacionesHelper.ProcesarHistorial(ctx, tabla, op, aplicar, usuarioId);

            var r = new Resultado { Tipo = op.Tipo, Filas = tabla.Rows.Count, Tolerante = op.Tolerante };
            var campos = Campos(op.Tipo);
            foreach (var c in campos.Where(c => c.Requerido))
                if (!op.Mapa.TryGetValue(c.Key, out var col) || string.IsNullOrEmpty(col))
                    r.Errores.Add($"Falta indicar la columna de «{c.Label}».");
            bool esVentas = op.Tipo == TipoVentas, esCxP = op.Tipo == TipoCxP;
            string campoTercero = esCxP ? "Proveedor" : "Cliente", codTercero = esCxP ? "ProveedorCodigo" : "ClienteCodigo", rtnTercero = esCxP ? "ProveedorRTN" : "ClienteRTN";

            if (!esVentas)
            {
                bool hayId = new[] { campoTercero, codTercero, rtnTercero }.Any(k => op.Mapa.TryGetValue(k, out var col) && !string.IsNullOrEmpty(col));
                if (!hayId) r.Errores.Add($"Indica al menos una columna para identificar al {(esCxP ? "proveedor" : "cliente")} (nombre, código o RTN).");
            }
            if (r.Errores.Count > 0) return r;

            var indice = esCxP ? IndiceProveedores(ctx) : IndiceClientes(ctx);
            string prefijo = op.Tipo switch { TipoCxC => "SI-", TipoCxP => "SI-CXP-", _ => "H-" };
            var existentes = ctx.Ventas.AsNoTracking().Where(v => v.LoteMigracion != null && v.Numero.StartsWith(prefijo)).Select(v => v.Numero).ToList()
                .Concat(esCxP ? ctx.OrdenesCompra.AsNoTracking().Where(o => o.LoteMigracion != null && o.Numero.StartsWith(prefijo)).Select(o => o.Numero).ToList() : new List<string>())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Líneas (solo ventas): documento → líneas
            var lineasPorDoc = new Dictionary<string, List<LineaPlan>>(StringComparer.OrdinalIgnoreCase);
            if (esVentas && op.Detalle != null)
                CargarLineas(op, lineasPorDoc, r);

            // ----- Fase 1: leer y validar -----
            var plan = new List<FilaPlan>();
            var numerosEnArchivo = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var documentosUsados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int n = 0;
            foreach (var fila in tabla.Rows)
            {
                n++;
                int nf = n + 1; // fila del archivo (la 1 es el encabezado)
                var doc = Valor(fila, op.Mapa, "Documento");
                if (doc == null) { r.Omitidos++; r.Error(nf, "no tiene número de documento."); continue; }

                var fechaTxt = Valor(fila, op.Mapa, "Fecha");
                var fecha = ParsearFecha(fechaTxt);
                if (fechaTxt != null && (fecha == null || !FechaPlausible(fecha.Value)))
                { r.Omitidos++; r.Error(nf, $"la fecha «{fechaTxt}» no es válida (usa dd/MM/yyyy)."); continue; }
                if (fecha == null)
                {
                    if (esVentas) { r.Omitidos++; r.Error(nf, "no tiene fecha."); continue; }
                    fecha = DateTime.Today;
                    r.Aviso(nf, "sin fecha: se usó la de hoy (el vencimiento y la antigüedad del saldo se calculan desde ahí).");
                }

                var p = new FilaPlan
                {
                    Fila = nf, Documento = doc, Fecha = fecha.Value,
                    TerceroCodigo = Valor(fila, op.Mapa, codTercero),
                    TerceroRtn = Valor(fila, op.Mapa, rtnTercero),
                    TerceroNombre = Valor(fila, op.Mapa, campoTercero),
                    Notas = Valor(fila, op.Mapa, "Notas")
                };

                var venceTxt = Valor(fila, op.Mapa, "Vencimiento");
                if (venceTxt != null)
                {
                    var v = ParsearFecha(venceTxt);
                    if (v == null) r.Aviso(nf, $"el vencimiento «{venceTxt}» no se entendió; se ignora.");
                    else p.Vence = v;
                }

                decimal? Dec(string campo)
                {
                    var t = Valor(fila, op.Mapa, campo);
                    if (t == null) return null;
                    var d = ParserHelper.ParsearDecimal(t);
                    if (d == null) r.Aviso(nf, $"«{t}» no es un número válido en {campo}; se ignora.");
                    return d;
                }

                if (esVentas)
                {
                    var estado = Valor(fila, op.Mapa, "Estado");
                    if (estado != null)
                    {
                        var e = Norm(estado);
                        if (e.Contains("anul") || e.Contains("cancel") || e == "true" && Norm(op.Mapa.GetValueOrDefault("Estado")).Contains("anul"))
                        { r.Omitidos++; continue; }
                    }
                    var total = Dec("Total");
                    if (total == null || total < 0) { r.Omitidos++; r.Error(nf, "el total falta o no es válido."); continue; }
                    if (total == 0) { r.Omitidos++; continue; }
                    p.Total = Math.Round(total.Value, 2);
                    p.Descuento = Math.Round(Dec("Descuento") ?? 0, 2);
                    p.Impuestos = Math.Round(Dec("Impuestos") ?? 0, 2);
                    p.Subtotal = Math.Round(Dec("Subtotal") ?? (p.Total - p.Impuestos), 2);
                    var saldoV = Dec("Saldo");
                    if (saldoV != null && saldoV > 0) p.Saldo = Math.Min(Math.Round(saldoV.Value, 2), p.Total);
                    var forma = Valor(fila, op.Mapa, "FormaPago");
                    if (forma != null) p.Notas = (p.Notas == null ? "" : p.Notas + " · ") + "Forma de pago: " + forma;

                    if (lineasPorDoc.TryGetValue(doc, out var ls)) p.Lineas = ls;
                }
                else
                {
                    var saldo = Dec("Saldo");
                    if (saldo == null) { r.Omitidos++; r.Error(nf, "el saldo falta o no es válido."); continue; }
                    if (saldo <= 0) { r.Omitidos++; r.Aviso(nf, "saldo en cero o negativo: no se importa (un saldo a favor se registra a mano)."); continue; }
                    p.Saldo = Math.Round(saldo.Value, 2);
                    var total = Dec("Total");
                    p.Total = total != null && total >= p.Saldo ? Math.Round(total.Value, 2) : p.Saldo;
                    if (total != null && total < p.Saldo) r.Aviso(nf, "el total original es menor que el saldo: se usó el saldo como total.");
                }

                if (string.IsNullOrWhiteSpace(p.TerceroNombre) && string.IsNullOrWhiteSpace(p.TerceroCodigo) && string.IsNullOrWhiteSpace(p.TerceroRtn))
                {
                    if (!esVentas) { r.Omitidos++; r.Error(nf, $"no trae {(esCxP ? "proveedor" : "cliente")}."); continue; }
                }
                else
                {
                    p.TerceroId = indice.Buscar(p.TerceroCodigo, p.TerceroRtn, p.TerceroNombre);
                    p.ClaveTercero = ClaveTercero(p.TerceroCodigo, p.TerceroRtn, p.TerceroNombre);
                    if (p.TerceroId == null)
                    {
                        bool puedeCrear = op.TerceroNoExiste == "crear" && !string.IsNullOrWhiteSpace(p.TerceroNombre);
                        if (!puedeCrear)
                        {
                            if (esVentas) { r.Aviso(nf, $"el cliente «{p.TerceroNombre ?? p.TerceroCodigo}» no existe: queda como consumidor final."); p.ClaveTercero = ""; }
                            else { r.Omitidos++; r.Error(nf, $"el {(esCxP ? "proveedor" : "cliente")} «{p.TerceroNombre ?? p.TerceroCodigo ?? p.TerceroRtn}» no existe y no se puede crear sin nombre."); continue; }
                        }
                    }
                }

                if (esVentas && p.Saldo > 0 && p.TerceroId == null && p.ClaveTercero.Length == 0)
                    r.Aviso(nf, $"el documento {doc} tiene saldo pendiente pero no cliente: se importa como venta pagada (no queda por cobrar).");

                // Número único del documento en el sistema
                var baseNum = Recortar(prefijo + doc, 36);
                var numero = baseNum;
                if (existentes.Contains(numero)) { r.YaImportados++; continue; }
                if (numerosEnArchivo.TryGetValue(baseNum, out var veces))
                {
                    numerosEnArchivo[baseNum] = veces + 1;
                    numero = baseNum + "-" + (veces + 1);
                    r.Aviso(nf, $"el documento «{doc}» aparece repetido en el archivo; se guarda como {numero}.");
                }
                else numerosEnArchivo[baseNum] = 1;
                p.Numero = numero;

                plan.Add(p);
                r.Monto += esVentas ? p.Total : p.Saldo;
            }

            if (esVentas && op.Detalle != null)
            {
                var docs = plan.Select(x => x.Documento).ToHashSet(StringComparer.OrdinalIgnoreCase);
                int sueltas = lineasPorDoc.Keys.Count(k => !docs.Contains(k));
                if (sueltas > 0) r.Aviso(0, $"{sueltas} documento(s) de la tabla de líneas no tienen su encabezado en la tabla de ventas y se ignoran.");
            }

            r.Creados = plan.Count;
            r.TercerosPorCrear = plan.Where(x => x.TerceroId == null && x.ClaveTercero.Length > 0).Select(x => x.ClaveTercero).Distinct().Count();

            if (!aplicar) return r;
            if (r.Errores.Count > 0 || plan.Count == 0)
            {
                if (plan.Count == 0 && r.Errores.Count == 0 && !r.Tolerante) r.Errores.Add("No hay nada nuevo que importar.");
                r.Creados = 0;
                return r;
            }

            // ----- Fase 2: guardar -----
            var lote = NuevoLote();
            r.Lote = lote;
            var propia = ctx.Database.CurrentTransaction == null;
            using var tx = propia ? ctx.Database.BeginTransaction() : null;
            try
            {
                // 2.1 terceros nuevos
                var creados = new Dictionary<string, int>();
                foreach (var grupo in plan.Where(x => x.TerceroId == null && x.ClaveTercero.Length > 0).GroupBy(x => x.ClaveTercero))
                {
                    var f = grupo.First();
                    var nombre = Recortar((f.TerceroNombre ?? f.TerceroCodigo ?? "Sin nombre").Trim(), 150);
                    var rtn = f.TerceroRtn == null ? null : Recortar(f.TerceroRtn, 20);
                    string? codigo = string.IsNullOrWhiteSpace(f.TerceroCodigo) ? null : Recortar(f.TerceroCodigo.Trim(), 20);
                    if (codigo != null && indice.CodigosUsados.Contains(codigo)) codigo = null;
                    if (codigo != null) indice.CodigosUsados.Add(codigo);
                    var nota = $"Creado por la migración ({lote}).";

                    if (esCxP)
                    {
                        var pr = new Proveedor { Nombre = nombre, Codigo = codigo, RTN = rtn, Notas = nota, UsuarioCreoId = usuarioId };
                        ctx.Proveedores.Add(pr); ctx.SaveChanges(); creados[grupo.Key] = pr.Id;
                    }
                    else
                    {
                        var cl = new Cliente { Nombre = nombre, Codigo = codigo, RTN = rtn, Notas = nota, UsuarioCreoId = usuarioId };
                        ctx.Clientes.Add(cl); ctx.SaveChanges(); creados[grupo.Key] = cl.Id;
                    }
                    r.TercerosCreados++;
                }
                foreach (var p in plan.Where(x => x.TerceroId == null && x.ClaveTercero.Length > 0))
                    p.TerceroId = creados[p.ClaveTercero];

                // 2.2 documentos
                int generico = 0;
                var productos = esVentas ? IndiceProductos(ctx) : null;
                var diasCredito = !esCxP
                    ? ctx.Clientes.AsNoTracking().Where(c => c.DiasCredito > 0).ToDictionary(c => c.Id, c => c.DiasCredito) : new Dictionary<int, int>();

                const int lote500 = 200;
                for (int i = 0; i < plan.Count; i += lote500)
                {
                    var tanda = plan.Skip(i).Take(lote500).ToList();

                    if (esCxP)
                    {
                        foreach (var p in tanda)
                        {
                            ctx.OrdenesCompra.Add(new OrdenCompra
                            {
                                Numero = p.Numero, ProveedorId = p.TerceroId!.Value, Fecha = p.Fecha, FechaRecepcion = p.Fecha,
                                Estado = "Recibida", Subtotal = p.Total, Total = p.Total, Saldo = p.Saldo,
                                Notas = $"Saldo inicial migrado — factura {p.Documento}" + (p.Vence != null ? $" — vence {p.Vence:dd/MM/yyyy}" : "") + (p.Notas != null ? " — " + p.Notas : ""),
                                FechaEntregaEstimada = p.Vence,
                                UsuarioCreoId = usuarioId, UsuarioRecibioId = usuarioId, LoteMigracion = lote
                            });
                        }
                        ctx.SaveChanges();
                    }
                    else if (!esVentas)
                    {
                        foreach (var p in tanda)
                        {
                            DateTime? vence = p.Vence;
                            if (vence == null && p.TerceroId != null && diasCredito.TryGetValue(p.TerceroId.Value, out var dias)) vence = p.Fecha.AddDays(dias);
                            ctx.Ventas.Add(new Venta
                            {
                                Numero = p.Numero, ClienteId = p.TerceroId, Fecha = p.Fecha,
                                Subtotal = p.Total, Total = p.Total, Estado = "Completada",
                                EsCredito = true, SaldoPendiente = p.Saldo, FechaVencimientoCredito = vence,
                                // MovimientoRegistrado=false para que Finanzas → Cobros permita cobrarla;
                                // el costo ya pertenece al sistema anterior.
                                MovimientoRegistrado = false, CostoRegistrado = true,
                                Notas = $"Saldo inicial migrado — factura {p.Documento}" + (p.Notas != null ? " — " + p.Notas : ""),
                                UsuarioCreoId = usuarioId, LoteMigracion = lote
                            });
                        }
                        ctx.SaveChanges();
                    }
                    else
                    {
                        var ventas = new List<(FilaPlan p, Venta v)>();
                        foreach (var p in tanda)
                        {
                            decimal costo = p.Lineas.Sum(l => l.Costo * l.Cantidad);
                            bool cred = p.Saldo > 0 && p.TerceroId != null;
                            DateTime? venceV = null;
                            if (cred)
                            {
                                venceV = p.Vence;
                                if (venceV == null && diasCredito.TryGetValue(p.TerceroId!.Value, out var dv)) venceV = p.Fecha.AddDays(dv);
                            }
                            var v = new Venta
                            {
                                Numero = p.Numero, ClienteId = p.TerceroId, Fecha = p.Fecha,
                                Subtotal = p.Subtotal, Descuento = p.Descuento, Impuestos = p.Impuestos, Total = p.Total,
                                Estado = "Completada", EsCredito = cred, SaldoPendiente = cred ? p.Saldo : 0,
                                FechaVencimientoCredito = venceV,
                                // Crédito con saldo: MovimientoRegistrado=false para poder cobrarla en Finanzas → Cobros.
                                MovimientoRegistrado = !cred, FechaMovimiento = cred ? null : p.Fecha,
                                CostoRegistrado = true, CostoTotalVendido = Math.Round(costo, 2),
                                Notas = $"Venta histórica migrada — documento {p.Documento}" + (p.Notas != null ? " — " + p.Notas : ""),
                                UsuarioCreoId = usuarioId, LoteMigracion = lote
                            };
                            ctx.Ventas.Add(v);
                            ventas.Add((p, v));
                        }
                        ctx.SaveChanges();

                        foreach (var (p, v) in ventas)
                        {
                            var lineas = p.Lineas.Count > 0 ? p.Lineas : new List<LineaPlan>
                            {
                                new() { Nombre = "Venta histórica (sin detalle de productos)", Cantidad = 1, Precio = p.Subtotal, Total = p.Total }
                            };
                            foreach (var l in lineas)
                            {
                                int? pid = productos!.Buscar(l.Codigo, l.Nombre);
                                if (pid == null)
                                {
                                    if (generico == 0) generico = ProductoGenerico(ctx);
                                    pid = generico;
                                }
                                var desc = l.Nombre ?? l.Codigo;
                                var sub = Math.Round(l.Cantidad * l.Precio - l.Descuento, 2);
                                ctx.DetalleVentas.Add(new DetalleVenta
                                {
                                    VentaId = v.Id, ProductoId = pid.Value, Descripcion = desc == null ? null : Recortar(desc, 250),
                                    Cantidad = l.Cantidad, PrecioUnitario = l.Precio, Descuento = l.Descuento,
                                    Subtotal = sub, Total = l.Total > 0 ? l.Total : sub,
                                    CostoUnitarioSnapshot = l.Costo, CostoTotalLinea = Math.Round(l.Costo * l.Cantidad, 2)
                                });
                                r.LineasCreadas++;
                            }
                        }
                        ctx.SaveChanges();
                    }
                    ctx.ChangeTracker.Clear();
                }

                tx?.Commit();
                r.Aplicado = true;
            }
            catch (Exception ex)
            {
                if (!propia) throw;   // dentro de una migración automática: se cancela todo
                tx?.Rollback();
                ctx.ChangeTracker.Clear();
                r.Errores.Add("No se guardó nada por un error inesperado: " + (ex.InnerException?.Message ?? ex.Message));
                r.Creados = 0; r.TercerosCreados = 0; r.LineasCreadas = 0; r.Lote = null;
            }
            return r;
        }

        // ---------- líneas de ventas ----------
        private static void CargarLineas(Opciones op, Dictionary<string, List<LineaPlan>> porDoc, Resultado r)
        {
            var det = op.Detalle!;
            foreach (var c in CamposDetalle().Where(c => c.Requerido))
                if (!op.MapaDetalle.TryGetValue(c.Key, out var col) || string.IsNullOrEmpty(col))
                { r.Errores.Add($"En la tabla de líneas falta indicar la columna de «{c.Label}»."); return; }

            int n = 0;
            foreach (var fila in det.Rows)
            {
                n++;
                var doc = Valor(fila, op.MapaDetalle, "Documento");
                if (doc == null) continue;
                var cant = ParserHelper.ParsearDecimal(Valor(fila, op.MapaDetalle, "Cantidad"));
                var precio = ParserHelper.ParsearDecimal(Valor(fila, op.MapaDetalle, "Precio"));
                if (cant == null || cant <= 0 || precio == null)
                { r.Aviso(0, $"Línea {n + 1} del documento {doc}: cantidad o precio no válidos; se ignora."); continue; }

                var l = new LineaPlan
                {
                    Codigo = Valor(fila, op.MapaDetalle, "ProductoCodigo"),
                    Nombre = Valor(fila, op.MapaDetalle, "ProductoNombre"),
                    Cantidad = cant.Value,
                    Precio = Math.Round(precio.Value, 4),
                    Descuento = Math.Round(ParserHelper.ParsearDecimal(Valor(fila, op.MapaDetalle, "Descuento")) ?? 0, 2),
                    Total = Math.Round(ParserHelper.ParsearDecimal(Valor(fila, op.MapaDetalle, "TotalLinea")) ?? 0, 2),
                    Costo = Math.Round(ParserHelper.ParsearDecimal(Valor(fila, op.MapaDetalle, "Costo")) ?? 0, 4)
                };
                if (!porDoc.TryGetValue(doc, out var lista)) porDoc[doc] = lista = new List<LineaPlan>();
                lista.Add(l);
            }
        }

        // ---------- productos ----------
        internal class IndiceProd
        {
            public Dictionary<string, int> PorCodigo = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> PorNombre = new();
            public int? Buscar(string? codigo, string? nombre)
            {
                if (!string.IsNullOrWhiteSpace(codigo) && PorCodigo.TryGetValue(codigo.Trim(), out var a)) return a;
                var n = Norm(nombre);
                if (n.Length > 0 && PorNombre.TryGetValue(n, out var b)) return b;
                // a veces el "código" del archivo es en realidad el nombre
                var c = Norm(codigo);
                if (c.Length > 0 && PorNombre.TryGetValue(c, out var d)) return d;
                return null;
            }
        }

        internal static IndiceProd IndiceProductos(ApplicationDbContext ctx)
        {
            var idx = new IndiceProd();
            foreach (var p in ctx.Productos.AsNoTracking().Select(p => new { p.Id, p.SKU, p.CodigoBarras, p.Nombre }).ToList())
            {
                if (!string.IsNullOrWhiteSpace(p.SKU)) idx.PorCodigo.TryAdd(p.SKU.Trim(), p.Id);
                if (!string.IsNullOrWhiteSpace(p.CodigoBarras)) idx.PorCodigo.TryAdd(p.CodigoBarras.Trim(), p.Id);
                var n = Norm(p.Nombre); if (n.Length > 0) idx.PorNombre.TryAdd(n, p.Id);
            }
            return idx;
        }

        /// <summary>Producto inactivo que representa «artículo del sistema anterior que no existe en el catálogo».</summary>
        internal static int ProductoGenerico(ApplicationDbContext ctx)
        {
            var p = ctx.Productos.FirstOrDefault(x => x.SKU == GenericoSku);
            if (p != null) return p.Id;
            p = new Producto
            {
                SKU = GenericoSku, Nombre = "Artículo histórico (migración)",
                Descripcion = "Producto técnico para ventas históricas cuyos artículos no existen en el catálogo actual. No lo uses para vender.",
                Activo = false, Stock = 0
            };
            ctx.Productos.Add(p);
            ctx.SaveChanges();
            return p.Id;
        }

        // ================= Kardex histórico =================
        private static Resultado ProcesarKardex(ApplicationDbContext ctx, TablaOrigen tabla, Opciones op, bool aplicar, int? usuarioId)
        {
            var r = new Resultado { Tipo = TipoKardex, Filas = tabla.Rows.Count, Tolerante = op.Tolerante };
            foreach (var c in Campos(TipoKardex).Where(c => c.Requerido))
                if (!op.Mapa.TryGetValue(c.Key, out var col) || string.IsNullOrEmpty(col))
                    r.Errores.Add($"Falta indicar la columna de «{c.Label}».");
            bool hayProd = new[] { "ProductoCodigo", "ProductoNombre" }.Any(k => op.Mapa.TryGetValue(k, out var col) && !string.IsNullOrEmpty(col));
            if (!hayProd) r.Errores.Add("Indica la columna del código o del nombre del producto.");
            if (r.Errores.Count > 0) return r;

            var productos = IndiceProductos(ctx);
            var lote = NuevoLote();
            var filas = new List<MovimientoInventario>();
            int n = 0;
            foreach (var fila in tabla.Rows)
            {
                n++; int nf = n + 1;
                var pid = productos.Buscar(Valor(fila, op.Mapa, "ProductoCodigo"), Valor(fila, op.Mapa, "ProductoNombre"));
                if (pid == null) { r.Omitidos++; r.Aviso(nf, "el producto no existe en el catálogo: se omite (importa primero los productos)."); continue; }

                var fechaTxt = Valor(fila, op.Mapa, "Fecha");
                var fecha = ParsearFecha(fechaTxt);
                if (fecha == null || !FechaPlausible(fecha.Value)) { r.Omitidos++; r.Error(nf, $"la fecha «{fechaTxt}» no es válida."); continue; }

                var cant = ParserHelper.ParsearDecimal(Valor(fila, op.Mapa, "Cantidad"));
                if (cant == null || cant == 0) { r.Omitidos++; continue; }

                var tipoTxt = Norm(Valor(fila, op.Mapa, "Tipo"));
                bool salida = tipoTxt.StartsWith("sal") || tipoTxt.Contains("salida") || tipoTxt.Contains("venta") || tipoTxt.Contains("baja") || tipoTxt.Contains("egreso") || tipoTxt.Contains("consumo") || tipoTxt.Contains("merma");
                bool entrada = tipoTxt.StartsWith("ent") || tipoTxt.Contains("entrada") || tipoTxt.Contains("compra") || tipoTxt.Contains("ingreso") || tipoTxt.Contains("devol");
                decimal firmada = cant.Value;
                if (salida && !entrada) firmada = -Math.Abs(firmada);
                else if (entrada && !salida) firmada = Math.Abs(firmada);

                var post = ParserHelper.ParsearDecimal(Valor(fila, op.Mapa, "StockResultante"));
                var costo = ParserHelper.ParsearDecimal(Valor(fila, op.Mapa, "Costo"));
                var nota = $"[{lote}] Histórico del sistema anterior" + (Valor(fila, op.Mapa, "Notas") is string t ? " — " + t : "");

                filas.Add(new MovimientoInventario
                {
                    ProductoId = pid.Value, Fecha = fecha.Value,
                    Tipo = firmada > 0 ? "Entrada" : "Salida",
                    Origen = OrigenKardex,
                    DocumentoNumero = Valor(fila, op.Mapa, "Documento") is string d ? Recortar(d, 50) : null,
                    Cantidad = firmada,
                    StockPosterior = post ?? 0,
                    StockAnterior = post != null ? post.Value - firmada : 0,
                    CostoUnitario = costo, UsuarioId = usuarioId, Notas = Recortar(nota, 300)
                });
            }

            r.Creados = filas.Count;
            if (!aplicar) return r;
            if (r.Errores.Count > 0 || filas.Count == 0)
            {
                if (filas.Count == 0 && r.Errores.Count == 0 && !r.Tolerante) r.Errores.Add("No hay movimientos válidos para importar.");
                r.Creados = 0; return r;
            }

            // Evitar duplicar si se importa dos veces el mismo archivo
            var existentes = ctx.MovimientosInventario.AsNoTracking().Where(m => m.Origen == OrigenKardex)
                .Select(m => new { m.ProductoId, m.Fecha, m.Cantidad, m.DocumentoNumero }).ToList()
                .Select(m => (m.ProductoId, m.Fecha, m.Cantidad, m.DocumentoNumero)).ToHashSet();
            int antes = filas.Count;
            filas = filas.Where(m => !existentes.Contains((m.ProductoId, m.Fecha, m.Cantidad, m.DocumentoNumero))).ToList();
            r.YaImportados = antes - filas.Count;
            r.Creados = filas.Count;
            if (filas.Count == 0) { if (!r.Tolerante) r.Errores.Add("Todos esos movimientos ya estaban importados."); return r; }

            var propiaK = ctx.Database.CurrentTransaction == null;
            using var txK = propiaK ? ctx.Database.BeginTransaction() : null;
            try
            {
                // Solo historial: NO se toca Producto.Stock.
                ctx.MovimientosInventario.AddRange(filas);
                ctx.SaveChanges();
                txK?.Commit();
                r.Aplicado = true; r.Lote = lote;
            }
            catch (Exception ex)
            {
                if (!propiaK) throw;
                txK?.Rollback(); ctx.ChangeTracker.Clear();
                r.Errores.Add("No se guardó nada por un error inesperado: " + (ex.InnerException?.Message ?? ex.Message));
                r.Creados = 0;
            }
            return r;
        }

        // ================= Lotes y deshacer =================
        public class LoteInfo
        {
            public string Lote { get; set; } = "";
            public int Ventas { get; set; }
            public int SaldosCxC { get; set; }
            public int SaldosCxP { get; set; }
            public int Kardex { get; set; }
            /// <summary>Empleados, cuentas, cotizaciones, compras y demás registros del lote.</summary>
            public int Otros { get; set; }
            public DateTime? Fecha => DateTime.TryParseExact(Lote.Replace("MIG-", ""), "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var f) ? f : null;
        }

        public static List<LoteInfo> ListarLotes(ApplicationDbContext ctx)
        {
            var lotes = new Dictionary<string, LoteInfo>();
            LoteInfo Get(string l) { if (!lotes.TryGetValue(l, out var x)) lotes[l] = x = new LoteInfo { Lote = l }; return x; }

            foreach (var g in ctx.Ventas.AsNoTracking().Where(v => v.LoteMigracion != null)
                .GroupBy(v => new { v.LoteMigracion, Cred = v.EsCredito }).Select(g => new { g.Key.LoteMigracion, g.Key.Cred, N = g.Count() }).ToList())
            {
                var l = Get(g.LoteMigracion!);
                if (g.Cred) l.SaldosCxC += g.N; else l.Ventas += g.N;
            }
            foreach (var g in ctx.OrdenesCompra.AsNoTracking().Where(o => o.LoteMigracion != null)
                .GroupBy(o => o.LoteMigracion).Select(g => new { Lote = g.Key, N = g.Count() }).ToList())
                Get(g.Lote!).SaldosCxP += g.N;

            foreach (var g in ctx.MovimientosInventario.AsNoTracking().Where(m => m.Origen == OrigenKardex && m.Notas != null && m.Notas.StartsWith("[MIG-"))
                .Select(m => m.Notas!.Substring(1, 19)).ToList().GroupBy(x => x))
                Get(g.Key).Kardex += g.Count();

            foreach (var par in MigracionOperacionesHelper.ContarRegistros(ctx))
                Get(par.Key).Otros += par.Value;

            return lotes.Values.OrderByDescending(l => l.Lote).ToList();
        }

        /// <summary>Borra lo importado en ese lote, salvo lo que ya tenga cobros/pagos/devoluciones/facturas (se informa).</summary>
        public static (int borrados, List<string> conservados) DeshacerLote(ApplicationDbContext ctx, string lote)
        {
            var conservados = new List<string>();
            int borrados = 0;
            if (string.IsNullOrWhiteSpace(lote) || !lote.StartsWith("MIG-")) return (0, conservados);

            using var tx = ctx.Database.BeginTransaction();
            try
            {
                var ventas = ctx.Ventas.Where(v => v.LoteMigracion == lote).ToList();
                foreach (var v in ventas)
                {
                    bool tiene = ctx.Pagos.Any(p => p.VentaId == v.Id)
                        || ctx.Facturas.Any(f => f.VentaId == v.Id)
                        || ctx.Devoluciones.Any(d => d.VentaId == v.Id)
                        || ctx.MovimientosFinancieros.Any(m => m.Origen == "Venta" && m.OrigenId == v.Id);
                    if (tiene) { conservados.Add($"Venta {v.Numero}: ya tiene cobros, facturas o movimientos."); continue; }
                    ctx.DetalleVentas.RemoveRange(ctx.DetalleVentas.Where(d => d.VentaId == v.Id));
                    ctx.Ventas.Remove(v);
                    borrados++;
                }

                var ordenes = ctx.OrdenesCompra.Where(o => o.LoteMigracion == lote).ToList();
                foreach (var o in ordenes)
                {
                    if (ctx.PagosProveedor.Any(p => p.OrdenCompraId == o.Id)) { conservados.Add($"Orden {o.Numero}: ya tiene pagos."); continue; }
                    ctx.DetalleOrdenesCompra.RemoveRange(ctx.DetalleOrdenesCompra.Where(d => d.OrdenCompraId == o.Id));
                    ctx.OrdenesCompra.Remove(o);
                    borrados++;
                }

                var marca = "[" + lote + "]";
                var kardex = ctx.MovimientosInventario.Where(m => m.Origen == OrigenKardex && m.Notas != null && m.Notas.StartsWith(marca)).ToList();
                borrados += kardex.Count;
                ctx.MovimientosInventario.RemoveRange(kardex);

                ctx.SaveChanges();
                borrados += MigracionOperacionesHelper.DeshacerRegistros(ctx, lote, conservados);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                ctx.ChangeTracker.Clear();
                throw;
            }
            return (borrados, conservados);
        }
    }
}
