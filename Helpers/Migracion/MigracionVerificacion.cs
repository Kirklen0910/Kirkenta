using Kirkenta.Data;
using Kirkenta.Helpers.Import;
using Microsoft.EntityFrameworkCore;
using static Kirkenta.Helpers.Migracion.MigracionHistorialHelper;

namespace Kirkenta.Helpers.Migracion
{
    /// <summary>Resultado de comparar, registro por registro, lo que venía con lo que quedó guardado.</summary>
    public class LineaVerificacion
    {
        public string Titulo { get; set; } = "";
        public string Tabla { get; set; } = "";
        public int Revisados { get; set; }
        public int TotalDiferencias { get; set; }
        public int Recortados { get; set; }
        /// <summary>Primeras diferencias, legibles («Factura 120: total 1,500.00 → 1,450.00»).</summary>
        public List<string> Diferencias { get; } = new();
        public bool Exacto => TotalDiferencias == 0 && Recortados == 0;

        internal void Diferencia(string texto)
        {
            TotalDiferencias++;
            if (Diferencias.Count < 50) Diferencias.Add(texto);
        }
    }

    /// <summary>
    /// Después de cada paso vuelve a leer de la base lo que se acaba de guardar y lo compara campo por campo
    /// con la fila del sistema anterior: precio, costo y existencia de cada producto; total, saldo y fecha
    /// de cada documento; nombre, RTN y crédito de cada cliente o proveedor.
    /// </summary>
    public static class MigracionVerificacion
    {
        /// <summary>Último id antes del paso: lo creado por el paso es lo que tiene un id mayor.</summary>
        public static int? Marcar(ApplicationDbContext ctx, PasoMigracion paso) => paso.Destino switch
        {
            "Productos" => ctx.Productos.Max(x => (int?)x.Id) ?? 0,
            "Clientes" => ctx.Clientes.Max(x => (int?)x.Id) ?? 0,
            "Proveedores" => ctx.Proveedores.Max(x => (int?)x.Id) ?? 0,
            "@ventas" or "@cxc" => ctx.Ventas.Max(x => (int?)x.Id) ?? 0,
            "@compras" or "@cxp" => ctx.OrdenesCompra.Max(x => (int?)x.Id) ?? 0,
            "@cotizaciones" => ctx.Cotizaciones.Max(x => (int?)x.Id) ?? 0,
            _ => null
        };

        public static LineaVerificacion? Verificar(ApplicationDbContext ctx, TablaOrigen tabla, PasoMigracion paso, int? marca)
        {
            if (marca == null) return null;
            var l = new LineaVerificacion { Titulo = paso.Titulo, Tabla = paso.TablaNombre };
            var m = paso.Mapa;
            switch (paso.Destino)
            {
                case "Productos": Productos(ctx, tabla, m, marca.Value, l); break;
                case "Clientes": case "Proveedores": Terceros(ctx, tabla, m, marca.Value, l, paso.Destino == "Proveedores"); break;
                default: Documentos(ctx, tabla, paso, marca.Value, l); break;
            }
            return l;
        }

        private static string Txt(decimal d) => d.ToString("#,##0.####");

        private static void Comparar(LineaVerificacion l, string quien, string campo, decimal esperado, decimal guardado)
        {
            if (Math.Abs(esperado - guardado) > 0.0001m) l.Diferencia($"{quien}: {campo} {Txt(esperado)} → {Txt(guardado)}");
        }

        private static void Comparar(LineaVerificacion l, string quien, string campo, string? esperado, string? guardado)
        {
            if (!string.Equals((esperado ?? "").Trim(), (guardado ?? "").Trim(), StringComparison.Ordinal))
                l.Diferencia($"{quien}: {campo} «{esperado}» → «{guardado}»");
        }

        /// <summary>Número que se esperaba guardar (0 si no viene o no es válido, igual que al migrar).</summary>
        private static decimal Num(Dictionary<string, string> f, Dictionary<string, string> m, string campo, bool sinNegativos = true)
        {
            var d = ParserHelper.ParsearDecimal(Valor(f, m, campo)) ?? 0;
            return sinNegativos && d < 0 ? 0 : d;
        }

        /// <summary>Claves que aparecen una sola vez en el archivo (las repetidas no se pueden comparar una a una).</summary>
        private static HashSet<string> Unicas(IEnumerable<string?> claves) =>
            claves.Where(c => c != null).GroupBy(c => c!, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() == 1)
                  .Select(g => g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        private static string Corte(string s, int max) => s.Length <= max ? s : s.Substring(0, max);

        // ---------- productos ----------
        private static void Productos(ApplicationDbContext ctx, TablaOrigen t, Dictionary<string, string> m, int marca, LineaVerificacion l)
        {
            var nuevos = ctx.Productos.AsNoTracking().Where(p => p.Id > marca)
                .Select(p => new { p.SKU, p.Nombre, p.PrecioVenta, p.PrecioCompra, p.PrecioMayorista, p.Stock }).ToList();
            var porSku = nuevos.Where(p => p.SKU != null).GroupBy(p => p.SKU!, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var porNombre = nuevos.GroupBy(p => Norm(p.Nombre)).ToDictionary(g => g.Key, g => g.First());
            var skus = Unicas(t.Rows.Select(f => Valor(f, m, "SKU")));
            var nombres = Unicas(t.Rows.Select(f => Valor(f, m, "Nombre") is string n ? Norm(n) : null));

            foreach (var f in t.Rows)
            {
                var nombre = Valor(f, m, "Nombre");
                if (nombre == null) continue;
                var sku = Valor(f, m, "SKU");
                if (sku != null && sku.Length > 50) sku = null;
                var p = sku != null ? (skus.Contains(sku) ? porSku.GetValueOrDefault(sku) : null)
                                    : (nombres.Contains(Norm(nombre)) ? porNombre.GetValueOrDefault(Norm(Corte(nombre, 150))) : null);
                if (p == null) continue;       // descartado o ya existía: lo cuenta la conciliación
                l.Revisados++;
                var quien = sku ?? nombre;
                Comparar(l, quien, "nombre", Corte(nombre, 150), p.Nombre);
                Comparar(l, quien, "precio de venta", Num(f, m, "PrecioVenta"), p.PrecioVenta);
                Comparar(l, quien, "costo", Num(f, m, "PrecioCompra"), p.PrecioCompra);
                if (m.ContainsKey("PrecioMayorista")) Comparar(l, quien, "precio mayorista", Num(f, m, "PrecioMayorista"), p.PrecioMayorista);
                if (m.ContainsKey("Stock")) Comparar(l, quien, "existencia", Num(f, m, "Stock"), p.Stock);
            }
        }

        // ---------- clientes / proveedores ----------
        private static void Terceros(ApplicationDbContext ctx, TablaOrigen t, Dictionary<string, string> m, int marca, LineaVerificacion l, bool proveedor)
        {
            var nuevos = proveedor
                ? ctx.Proveedores.AsNoTracking().Where(p => p.Id > marca).Select(p => new { p.Codigo, p.Nombre, p.RTN, p.LimiteCredito, p.DiasCredito }).ToList()
                : ctx.Clientes.AsNoTracking().Where(p => p.Id > marca).Select(p => new { p.Codigo, p.Nombre, p.RTN, p.LimiteCredito, p.DiasCredito }).ToList();
            var porCodigo = nuevos.Where(p => p.Codigo != null).GroupBy(p => p.Codigo!, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var porNombre = nuevos.GroupBy(p => Norm(p.Nombre)).ToDictionary(g => g.Key, g => g.First());
            var nombres = Unicas(t.Rows.Select(f => (Valor(f, m, "Nombre") ?? Valor(f, m, "RazonSocial")) is string n ? Norm(n) : null));

            foreach (var f in t.Rows)
            {
                var nombre = Valor(f, m, "Nombre") ?? Valor(f, m, "RazonSocial");
                if (nombre == null || !nombres.Contains(Norm(nombre))) continue;
                var codigo = Valor(f, m, "Codigo");
                var x = (codigo != null ? porCodigo.GetValueOrDefault(codigo) : null) ?? porNombre.GetValueOrDefault(Norm(Corte(nombre, 150)));
                if (x == null) continue;
                l.Revisados++;
                var quien = codigo ?? nombre;
                Comparar(l, quien, "nombre", Corte(nombre, 150), x.Nombre);
                if (m.ContainsKey("RTN")) Comparar(l, quien, "RTN", Valor(f, m, "RTN") is string r ? Corte(r, 20) : null, x.RTN);
                if (m.ContainsKey("LimiteCredito")) Comparar(l, quien, "límite de crédito", Num(f, m, "LimiteCredito"), x.LimiteCredito);
                if (m.ContainsKey("DiasCredito")) Comparar(l, quien, "días de crédito", Math.Truncate(Num(f, m, "DiasCredito")), x.DiasCredito);
            }
        }

        // ---------- documentos ----------
        private record Doc(string Numero, DateTime Fecha, decimal Total, decimal Saldo);

        private static void Documentos(ApplicationDbContext ctx, TablaOrigen t, PasoMigracion paso, int marca, LineaVerificacion l)
        {
            var m = paso.Mapa;
            (string prefijo, int max, bool saldo) = paso.Destino switch
            {
                "@ventas" => ("H-", 36, false),
                "@cxc" => ("SI-", 36, true),
                "@cxp" => ("SI-CXP-", 36, true),
                "@compras" => ("H-CMP-", 50, false),
                _ => ("H-COT-", 50, false)
            };
            var guardados = (paso.Destino switch
            {
                "@ventas" or "@cxc" => ctx.Ventas.AsNoTracking().Where(v => v.Id > marca).Select(v => new Doc(v.Numero, v.Fecha, v.Total, v.SaldoPendiente)).ToList(),
                "@compras" or "@cxp" => ctx.OrdenesCompra.AsNoTracking().Where(v => v.Id > marca).Select(v => new Doc(v.Numero, v.Fecha, v.Total, v.Saldo)).ToList(),
                _ => ctx.Cotizaciones.AsNoTracking().Where(v => v.Id > marca).Select(v => new Doc(v.Numero, v.Fecha, v.Total, 0m)).ToList()
            }).GroupBy(d => d.Numero, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var docs = Unicas(t.Rows.Select(f => Valor(f, m, "Documento")?.Trim()));

            foreach (var f in t.Rows)
            {
                var doc = Valor(f, m, "Documento")?.Trim();
                if (doc == null || !docs.Contains(doc) || !guardados.TryGetValue(Corte(prefijo + doc, max), out var g)) continue;
                l.Revisados++;
                var quien = "Documento " + doc;
                if (m.ContainsKey("Fecha") && ParsearFecha(Valor(f, m, "Fecha")) is DateTime fe && fe != g.Fecha)
                    l.Diferencia($"{quien}: fecha {fe:dd/MM/yyyy HH:mm} → {g.Fecha:dd/MM/yyyy HH:mm}");
                if (saldo)
                {
                    var s = Math.Round(Num(f, m, "Saldo"), 2, MidpointRounding.AwayFromZero);
                    Comparar(l, quien, "saldo", s, g.Saldo);
                    if (m.ContainsKey("Total") && Math.Round(Num(f, m, "Total"), 2, MidpointRounding.AwayFromZero) is var tot && tot >= s) Comparar(l, quien, "total", tot, g.Total);
                }
                else
                {
                    Comparar(l, quien, "total", Math.Round(Num(f, m, "Total"), 2, MidpointRounding.AwayFromZero), g.Total);
                    if (paso.Destino == "@compras" && m.ContainsKey("Saldo"))
                        Comparar(l, quien, "saldo", Math.Min(Math.Round(Num(f, m, "Saldo"), 2, MidpointRounding.AwayFromZero), Math.Round(Num(f, m, "Total"), 2, MidpointRounding.AwayFromZero)), g.Saldo);
                }
            }
        }
    }
}
