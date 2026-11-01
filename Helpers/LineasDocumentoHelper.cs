using Kirkenta.Data;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Validación servidor de las líneas de Cotización / Pedido.
    /// El navegador manda cantidades, precios y descuentos: nunca se confía ciegamente.
    /// </summary>
    public static class LineasDocumentoHelper
    {
        public readonly record struct Linea(int ProductoId, decimal Cantidad, decimal PrecioUnitario, decimal Descuento, decimal ImpuestoPorcentaje);

        /// <summary>Devuelve null si todo es válido, o el mensaje de error.</summary>
        public static string? Validar(ApplicationDbContext context, IReadOnlyList<Linea> lineas, bool soloActivos = true)
        {
            if (lineas == null || lineas.Count == 0)
                return "Agrega al menos un producto";

            var ids = lineas.Select(l => l.ProductoId).Distinct().ToList();
            var activos = context.Productos
                .Where(p => ids.Contains(p.Id) && (!soloActivos || p.Activo))
                .Select(p => p.Id)
                .ToList();

            for (int i = 0; i < lineas.Count; i++)
            {
                var l = lineas[i];
                var n = i + 1;

                if (!activos.Contains(l.ProductoId))
                    return $"Línea {n}: el producto no existe" + (soloActivos ? " o está inactivo" : "") + "";
                if (l.Cantidad <= 0)
                    return $"Línea {n}: la cantidad debe ser mayor a cero";
                if (l.Cantidad > 1_000_000m)
                    return $"Línea {n}: cantidad fuera de rango";
                if (l.PrecioUnitario < 0)
                    return $"Línea {n}: el precio no puede ser negativo";
                if (l.Descuento < 0)
                    return $"Línea {n}: el descuento no puede ser negativo";
                if (l.Descuento > l.Cantidad * l.PrecioUnitario)
                    return $"Línea {n}: el descuento no puede ser mayor al subtotal de la línea";
                if (l.ImpuestoPorcentaje < 0 || l.ImpuestoPorcentaje > 100)
                    return $"Línea {n}: el impuesto debe estar entre 0 y 100%";
            }

            return null;
        }
    }
}
