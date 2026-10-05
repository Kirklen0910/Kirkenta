using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Helper para cálculo y desglose de ISV/IVA.
    /// NO hardcodea tasas: siempre lee del catálogo Impuestos.
    /// Soporta monto de envío que se suma al subtotal y se grava con la tasa predeterminada.
    /// </summary>
    public static class ISVHelper
    {
        /// <summary>
        /// Resultado del cálculo de ISV agrupado por tasa.
        /// </summary>
        public class ResultadoISV
        {
            public Dictionary<decimal, decimal> BasePorTasa { get; set; } = new();
            public Dictionary<decimal, decimal> ISVPorTasa { get; set; } = new();

            public decimal BaseGravableTotal { get; set; }
            public decimal BaseExentaTotal { get; set; }
            public decimal ISVTotal { get; set; }
            public decimal Subtotal { get; set; }
            public decimal MontoEnvio { get; set; }
            public decimal Total { get; set; }
            public decimal Descuento { get; set; }

            public List<decimal> TasasAplicadas => BasePorTasa.Keys.OrderBy(t => t).ToList();
        }

        /// <summary>
        /// Calcula el ISV de una lista de items + un monto de envío opcional.
        /// El envío se grava con la tasa predeterminada del catálogo.
        /// </summary>
        public static ResultadoISV Calcular(
            ApplicationDbContext context,
            List<ItemParaISV> items,
            decimal descuentoGlobal = 0,
            decimal montoEnvio = 0)
        {
            var resultado = new ResultadoISV();
            resultado.Descuento = descuentoGlobal;
            resultado.MontoEnvio = montoEnvio;

            var impuestos = context.Impuestos
                .Where(i => i.Activo)
                .ToList();

            var impuestosDict = impuestos.ToDictionary(i => i.Id, i => i.Porcentaje);

            var impuestoPredeterminado = impuestos
                .FirstOrDefault(i => i.EsPredeterminado)?.Porcentaje ?? 0m;

            // ===== ITEMS NORMALES =====
            foreach (var item in items)
            {
                var bruto = item.Cantidad * item.PrecioUnitario;
                var neto = bruto - item.Descuento;

                if (neto <= 0) continue;

                decimal tasa = 0m;
                if (item.ImpuestoId.HasValue && impuestosDict.ContainsKey(item.ImpuestoId.Value))
                {
                    tasa = impuestosDict[item.ImpuestoId.Value];
                }
                else
                {
                    tasa = impuestoPredeterminado;
                }

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

            // ===== ENVÍO (se grava con la tasa predeterminada) =====
            if (montoEnvio > 0)
            {
                if (impuestoPredeterminado > 0)
                {
                    if (!resultado.BasePorTasa.ContainsKey(impuestoPredeterminado))
                    {
                        resultado.BasePorTasa[impuestoPredeterminado] = 0;
                        resultado.ISVPorTasa[impuestoPredeterminado] = 0;
                    }
                    resultado.BasePorTasa[impuestoPredeterminado] += montoEnvio;
                    resultado.BaseGravableTotal += montoEnvio;
                }
                else
                {
                    resultado.BaseExentaTotal += montoEnvio;
                }

                resultado.Subtotal += montoEnvio;
            }

            // ===== CALCULAR ISV POR TASA =====
            foreach (var tasa in resultado.BasePorTasa.Keys.ToList())
            {
                var baseTasa = resultado.BasePorTasa[tasa];
                var isv = Math.Round(baseTasa * (tasa / 100m), 2);
                resultado.ISVPorTasa[tasa] = isv;
                resultado.ISVTotal += isv;
            }

            resultado.Total = Math.Round(resultado.Subtotal + resultado.ISVTotal - descuentoGlobal, 2);
            if (resultado.Total < 0) resultado.Total = 0;

            return resultado;
        }

        /// <summary>
        /// Devuelve la tasa de ISV que aplica a un producto (o la predeterminada).
        /// </summary>
        public static decimal ObtenerTasaProducto(ApplicationDbContext context, int? impuestoId)
        {
            if (impuestoId.HasValue)
            {
                var imp = context.Impuestos
                    .FirstOrDefault(i => i.Id == impuestoId.Value && i.Activo);
                if (imp != null) return imp.Porcentaje;
            }

            var predeterminado = context.Impuestos
                .FirstOrDefault(i => i.EsPredeterminado && i.Activo);
            return predeterminado?.Porcentaje ?? 0m;
        }

        /// <summary>
        /// Recalcula los items de una factura usando el catálogo de impuestos.
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
        /// Considera el monto de envío en el total.
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

                if (factura.Estado == "Emitida" || factura.Estado == "PagadaParcial")
                {
                    factura.Saldo = total;
                }
            }

            return cambio;
        }

        /// <summary>
        /// Obtiene las tasas activas del catálogo.
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
    /// Item mínimo para calcular ISV.
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