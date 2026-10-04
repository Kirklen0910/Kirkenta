using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Helper para cálculo y desglose de ISV/IVA.
    /// NO hardcodea tasas: siempre lee del catálogo Impuestos.
    /// Si el SAR activa una nueva tasa, se crea desde /Impuestos y ya queda disponible.
    /// </summary>
    public static class ISVHelper
    {
        /// <summary>
        /// Resultado del cálculo de ISV agrupado por tasa.
        /// </summary>
        public class ResultadoISV
        {
            /// <summary>
            /// Base gravable por cada tasa (ej: {15: 1000, 18: 500})
            /// </summary>
            public Dictionary<decimal, decimal> BasePorTasa { get; set; } = new();

            /// <summary>
            /// Monto de ISV por cada tasa (ej: {15: 150, 18: 90})
            /// </summary>
            public Dictionary<decimal, decimal> ISVPorTasa { get; set; } = new();

            /// <summary>
            /// Base gravable total (suma de todas las bases)
            /// </summary>
            public decimal BaseGravableTotal { get; set; }

            /// <summary>
            /// Base exenta total (productos sin impuesto o con tasa 0)
            /// </summary>
            public decimal BaseExentaTotal { get; set; }

            /// <summary>
            /// ISV total a pagar (suma de todos los ISV por tasa)
            /// </summary>
            public decimal ISVTotal { get; set; }

            /// <summary>
            /// Subtotal antes de impuestos (base gravable + base exenta)
            /// </summary>
            public decimal Subtotal { get; set; }

            /// <summary>
            /// Total de la factura (subtotal + ISV - descuento)
            /// </summary>
            public decimal Total { get; set; }

            /// <summary>
            /// Descuento aplicado
            /// </summary>
            public decimal Descuento { get; set; }

            /// <summary>
            /// Lista de tasas que aplican en esta factura (para iterar en la vista)
            /// </summary>
            public List<decimal> TasasAplicadas => BasePorTasa.Keys.OrderBy(t => t).ToList();
        }

        /// <summary>
        /// Calcula el ISV de una lista de items (a nivel de línea).
        /// Cada item debe traer: Cantidad, PrecioUnitario, Descuento, ImpuestoId del producto.
        /// </summary>
        public static ResultadoISV Calcular(
            ApplicationDbContext context,
            List<ItemParaISV> items,
            decimal descuentoGlobal = 0)
        {
            var resultado = new ResultadoISV();
            resultado.Descuento = descuentoGlobal;

            // Cargar catálogo de impuestos en memoria
            var impuestos = context.Impuestos
                .Where(i => i.Activo)
                .ToList();

            var impuestosDict = impuestos.ToDictionary(i => i.Id, i => i.Porcentaje);

            // Impuesto predeterminado (si el producto no tiene uno)
            var impuestoPredeterminado = impuestos
                .FirstOrDefault(i => i.EsPredeterminado)?.Porcentaje ?? 0m;

            foreach (var item in items)
            {
                var bruto = item.Cantidad * item.PrecioUnitario;
                var neto = bruto - item.Descuento;

                if (neto <= 0) continue;

                // Determinar la tasa de este item
                decimal tasa = 0m;
                if (item.ImpuestoId.HasValue && impuestosDict.ContainsKey(item.ImpuestoId.Value))
                {
                    tasa = impuestosDict[item.ImpuestoId.Value];
                }
                else
                {
                    tasa = impuestoPredeterminado;
                }

                // Acumular base
                if (tasa > 0)
                {
                    if (!resultado.BasePorTasa.ContainsKey(tasa))
                    {
                        resultado.BasePorTasa[tasa] = 0;
                        resultado.ISVPorTasa[tasa] = 0;
                    }
                    resultado.BasePorTasa[tasa] += neto;
                    resultado.BaseGravableTotal += neto;
                }
                else
                {
                    resultado.BaseExentaTotal += neto;
                }

                resultado.Subtotal += neto;
            }

            // Calcular ISV por tasa
            foreach (var tasa in resultado.BasePorTasa.Keys.ToList())
            {
                var baseTasa = resultado.BasePorTasa[tasa];
                var isv = Math.Round(baseTasa * (tasa / 100m), 2);
                resultado.ISVPorTasa[tasa] = isv;
                resultado.ISVTotal += isv;
            }

            // Total final
            resultado.Total = Math.Round(resultado.Subtotal + resultado.ISVTotal - descuentoGlobal, 2);
            if (resultado.Total < 0) resultado.Total = 0;

            return resultado;
        }

        /// <summary>
        /// Calcula el ISV de un único item (usado al agregar al carrito del POS).
        /// Devuelve la tasa que aplica.
        /// </summary>
        public static decimal ObtenerTasaProducto(ApplicationDbContext context, int? impuestoId)
        {
            if (impuestoId.HasValue)
            {
                var imp = context.Impuestos
                    .FirstOrDefault(i => i.Id == impuestoId.Value && i.Activo);
                if (imp != null) return imp.Porcentaje;
            }

            // Fallback: predeterminado
            var predeterminado = context.Impuestos
                .FirstOrDefault(i => i.EsPredeterminado && i.Activo);
            return predeterminado?.Porcentaje ?? 0m;
        }

        /// <summary>
        /// Recalcula los items de una factura usando el catálogo de impuestos.
        /// Se usa para sincronizar cuando un producto cambió de tasa.
        /// Devuelve true si hubo cambios.
        /// </summary>
        public static bool SincronizarItems(
            ApplicationDbContext context,
            List<DetalleFactura> items)
        {
            var productos = context.Productos
                .Where(p => items.Select(i => i.ProductoId).Contains(p.Id))
                .ToDictionary(p => p.Id, p => p.ImpuestoId);

            var impuestos = context.Impuestos
                .Where(i => i.Activo)
                .ToDictionary(i => i.Id, i => i.Porcentaje);

            var predeterminado = context.Impuestos
                .FirstOrDefault(i => i.EsPredeterminado && i.Activo)?.Porcentaje ?? 0m;

            bool cambio = false;
            foreach (var item in items)
            {
                decimal tasaCorrecta = 0m;
                var prodImpuestoId = productos.GetValueOrDefault(item.ProductoId);

                if (prodImpuestoId.HasValue && impuestos.ContainsKey(prodImpuestoId.Value))
                    tasaCorrecta = impuestos[prodImpuestoId.Value];
                else
                    tasaCorrecta = predeterminado;

                if (item.ImpuestoPorcentaje != tasaCorrecta)
                {
                    item.ImpuestoPorcentaje = tasaCorrecta;
                    cambio = true;
                }
            }

            return cambio;
        }

        /// <summary>
        /// Recalcula el ISV de una factura existente (re-sincronizando con el catálogo)
        /// y devuelve true si hubo cambios que se deban guardar.
        /// </summary>
        public static bool RecalcularFactura(
            ApplicationDbContext context,
            Factura factura,
            List<DetalleFactura> items)
        {
            bool cambio = SincronizarItems(context, items);

            if (cambio)
            {
                decimal subtotal = 0, descuento = 0, impuestos = 0, total = 0;

                foreach (var item in items)
                {
                    var bruto = item.Cantidad * item.PrecioUnitario;
                    var neto = bruto - item.Descuento;
                    var isv = Math.Round(neto * (item.ImpuestoPorcentaje / 100m), 2);

                    item.Subtotal = neto;
                    item.Total = neto + isv;

                    subtotal += bruto;
                    descuento += item.Descuento;
                    impuestos += isv;
                    total += item.Total;
                }

                factura.Subtotal = subtotal;
                factura.Descuento = descuento;
                factura.Impuestos = impuestos;
                factura.Total = total;

                // Ajustar el saldo si aún no está pagada
                if (factura.Estado == "Emitida" || factura.Estado == "PagadaParcial")
                {
                    factura.Saldo = total;
                }
            }

            return cambio;
        }

        /// <summary>
        /// Obtiene las tasas activas del catálogo (para iterar en la vista).
        /// </summary>
        public static List<Impuesto> ObtenerTasasActivas(ApplicationDbContext context)
        {
            return context.Impuestos
                .Where(i => i.Activo)
                .OrderBy(i => i.Porcentaje)
                .ToList();
        }
    }

    /// <summary>
    /// Item mínimo para calcular ISV (se usa desde cualquier origen: factura, cotización, venta).
    /// </summary>
    public class ItemParaISV
    {
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public int? ImpuestoId { get; set; }
    }
}