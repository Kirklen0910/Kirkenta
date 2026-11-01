using System.Globalization;

namespace Kirkenta.Helpers
{
    /// <summary>Tamaño de etiqueta o formato de hoja para imprimir códigos de barras.</summary>
    public record FormatoEtiqueta(string Clave, string Nombre, double AnchoMm, double AltoMm, int Columnas, int Filas,
        double MargenSupMm, double MargenIzqMm, double SepXMm, double SepYMm, double PaginaAnchoMm, double PaginaAltoMm)
    {
        public int PorPagina => Columnas * Filas;
        public bool EsRollo => PorPagina == 1;
    }

    public static class EtiquetasHelper
    {
        public const int MaxEtiquetas = 1000;

        public static readonly IReadOnlyList<FormatoEtiqueta> Formatos = new List<FormatoEtiqueta>
        {
            // Rollos (impresoras térmicas): cada etiqueta es una página del tamaño exacto
            new("50x25", "Rollo 50 × 25 mm (térmica)", 50, 25, 1, 1, 0, 0, 0, 0, 50, 25),
            new("40x30", "Rollo 40 × 30 mm (térmica)", 40, 30, 1, 1, 0, 0, 0, 0, 40, 30),
            new("60x40", "Rollo 60 × 40 mm (térmica)", 60, 40, 1, 1, 0, 0, 0, 0, 60, 40),
            new("30x20", "Rollo 30 × 20 mm (pequeña)", 30, 20, 1, 1, 0, 0, 0, 0, 30, 20),
            // Hojas adhesivas tamaño carta/A4 (impresora láser o de inyección)
            new("hoja21", "Hoja A4 · 21 etiquetas (63.5 × 38.1 mm)", 63.5, 38.1, 3, 7, 15.1, 7.2, 2.5, 0, 210, 297),
            new("hoja24", "Hoja A4 · 24 etiquetas (70 × 37 mm)", 70, 37, 3, 8, 0, 0, 0, 0, 210, 297),
            new("hoja65", "Hoja A4 · 65 etiquetas (38.1 × 21.2 mm)", 38.1, 21.2, 5, 13, 10.7, 4.7, 2.5, 0, 210, 297),
        };

        public static FormatoEtiqueta Formato(string? clave)
            => Formatos.FirstOrDefault(f => f.Clave == clave) ?? Formatos[0];

        /// <summary>
        /// Convierte "12:3,15:1" (id:cantidad) en pares válidos. Ignora basura, une repetidos y limita el total.
        /// </summary>
        public static List<(int Id, int Cantidad)> ParsearItems(string? texto, int maximo = MaxEtiquetas)
        {
            var resultado = new List<(int, int)>();
            if (string.IsNullOrWhiteSpace(texto)) return resultado;

            var acumulado = new Dictionary<int, int>();
            var orden = new List<int>();
            foreach (var parte in texto.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var par = parte.Split(':', 2);
                if (!int.TryParse(par[0], NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0) continue;
                int cant = 1;
                if (par.Length == 2 && !int.TryParse(par[1], NumberStyles.None, CultureInfo.InvariantCulture, out cant)) continue;
                if (cant <= 0) continue;
                if (!acumulado.ContainsKey(id)) { acumulado[id] = 0; orden.Add(id); }
                acumulado[id] += cant;
            }

            int total = 0;
            foreach (var id in orden)
            {
                var c = Math.Min(acumulado[id], maximo - total);
                if (c <= 0) break;
                resultado.Add((id, c));
                total += c;
            }
            return resultado;
        }

        /// <summary>Parte las etiquetas en páginas según cuántas caben por hoja.</summary>
        public static List<List<T>> EnPaginas<T>(IEnumerable<T> etiquetas, int porPagina)
        {
            var paginas = new List<List<T>>();
            foreach (var e in etiquetas)
            {
                if (paginas.Count == 0 || paginas[^1].Count >= porPagina) paginas.Add(new List<T>());
                paginas[^1].Add(e);
            }
            return paginas;
        }

        /// <summary>Texto que se codifica: el código de barras del producto o, si no tiene, su SKU.</summary>
        public static string? CodigoDe(string? codigoBarras, string? sku)
        {
            var c = codigoBarras?.Trim();
            if (!string.IsNullOrEmpty(c)) return c;
            var s = sku?.Trim();
            return string.IsNullOrEmpty(s) ? null : s;
        }

        public static string Mm(double v) => v.ToString("0.##", CultureInfo.InvariantCulture) + "mm";
    }
}
