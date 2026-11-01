using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.Finanzas
{
    public static class ISVHelper
    {
        public class ResultadoISV
        {
            public Dictionary<decimal, decimal> BasePorTasa { get; set; } = new();
            public Dictionary<decimal, decimal> ISVPorTasa { get; set; } = new();

            public decimal BaseGravableTotal { get; set; }
            public decimal BaseExentaTotal { get; set; }
            /// <summary>Base de ventas a clientes exonerados (no paga ISV, distinta de la exenta).</summary>
            public decimal BaseExoneradaTotal { get; set; }
            public decimal ISVTotal { get; set; }
            public decimal Subtotal { get; set; }
            public decimal MontoEnvio { get; set; }
            public decimal Total { get; set; }
            public decimal Descuento { get; set; }
            public decimal DescuentoAplicadoBase { get; set; }

            public List<decimal> TasasAplicadas => BasePorTasa.Keys.OrderBy(t => t).ToList();
        }

        /// <summary>
        /// Calcula el ISV de una lista de items + un monto de envío opcional.
        /// 🔒 FIX: El descuento global se aplica ANTES de calcular el ISV.
        /// Se prorratea proporcionalmente sobre cada línea y sobre el envío,
        /// para que la base gravable y el ISV reflejen el descuento real.
        /// </summary>
        public static ResultadoISV Calcular(
            ApplicationDbContext context,
            List<ItemParaISV> items,
            decimal descuentoGlobal = 0,
            decimal montoEnvio = 0,
            bool exonerado = false)
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

            // 1. Calcular bases brutas por línea (sin descuento global)
            decimal baseBrutaItems = 0;
            var lineas = new List<(decimal neto, decimal tasa)>();

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

                // Cliente exonerado: ninguna línea paga ISV
                if (exonerado) tasa = 0m;

                lineas.Add((neto, tasa));
                baseBrutaItems += neto;
            }

            decimal baseBrutaTotal = baseBrutaItems + montoEnvio;

            // 2. Calcular factor de descuento global
            // Si el descuento es mayor que la base, se aplica el máximo (base completa)
            decimal descuentoAplicable = descuentoGlobal;
            if (descuentoAplicable < 0) descuentoAplicable = 0;
            if (descuentoAplicable > baseBrutaTotal) descuentoAplicable = baseBrutaTotal;

            decimal factorDescuento = baseBrutaTotal > 0
                ? (baseBrutaTotal - descuentoAplicable) / baseBrutaTotal
                : 0;

            // 3. Aplicar el factor proporcionalmente a cada línea
            foreach (var (neto, tasa) in lineas)
            {
                var netoConDescuento = Math.Round(neto * factorDescuento, 2);
                if (netoConDescuento <= 0) continue;

                if (tasa > 0)
                {
                    if (!resultado.BasePorTasa.ContainsKey(tasa))
                    {
                        resultado.BasePorTasa[tasa] = 0;
                        resultado.ISVPorTasa[tasa] = 0;
                    }
                    resultado.BasePorTasa[tasa] += netoConDescuento;
                    resultado.BaseGravableTotal += netoConDescuento;
                }
                else if (exonerado)
                {
                    resultado.BaseExoneradaTotal += netoConDescuento;
                }
                else
                {
                    resultado.BaseExentaTotal += netoConDescuento;
                }

                resultado.Subtotal += netoConDescuento;
            }

            // 4. Aplicar el descuento proporcional al envío
            if (montoEnvio > 0)
            {
                var envioConDescuento = Math.Round(montoEnvio * factorDescuento, 2);

                if (impuestoPredeterminado > 0 && !exonerado)
                {
                    if (!resultado.BasePorTasa.ContainsKey(impuestoPredeterminado))
                    {
                        resultado.BasePorTasa[impuestoPredeterminado] = 0;
                        resultado.ISVPorTasa[impuestoPredeterminado] = 0;
                    }
                    resultado.BasePorTasa[impuestoPredeterminado] += envioConDescuento;
                    resultado.BaseGravableTotal += envioConDescuento;
                }
                else if (exonerado)
                {
                    resultado.BaseExoneradaTotal += envioConDescuento;
                }
                else
                {
                    resultado.BaseExentaTotal += envioConDescuento;
                }

                resultado.Subtotal += envioConDescuento;
            }

            // 5. Calcular ISV por tasa sobre la base ya descontada
            foreach (var tasa in resultado.BasePorTasa.Keys.ToList())
            {
                var baseTasa = resultado.BasePorTasa[tasa];
                var isv = Math.Round(baseTasa * (tasa / 100m), 2);
                resultado.ISVPorTasa[tasa] = isv;
                resultado.ISVTotal += isv;
            }

            resultado.DescuentoAplicadoBase = descuentoAplicable;
            resultado.Total = Math.Round(resultado.Subtotal + resultado.ISVTotal, 2);

            return resultado;
        }

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

        public static bool RecalcularFactura(
            ApplicationDbContext context,
            Factura factura,
            List<DetalleFactura> items)
        {
            // Una factura exonerada nunca se re-grava con el impuesto del producto
            if (factura.EsExonerada) return false;

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
        /// Desglose fiscal de una factura ya emitida (importe exento, gravado por tasa e ISV
        /// por tasa). Usa los porcentajes GUARDADOS en cada línea de la factura, así que una
        /// factura vieja no cambia si después se modifica el impuesto del producto.
        /// Reproduce la misma regla de la venta: el descuento general se prorratea antes del
        /// ISV y el envío se grava con la tasa que traía la factura (se deduce del ISV total;
        /// si no se puede deducir, usa la tasa predeterminada).
        /// </summary>
        public static ResultadoISV DesglosarFactura(
            ApplicationDbContext context,
            Factura factura,
            IEnumerable<DetalleFactura> items)
        {
            var r = new ResultadoISV
            {
                Descuento = factura.Descuento,
                MontoEnvio = factura.MontoEnvio
            };

            var lineas = items
                .Select(i => (neto: i.Subtotal > 0 ? i.Subtotal : (i.Cantidad * i.PrecioUnitario - i.Descuento),
                              tasa: i.ImpuestoPorcentaje))
                .Where(l => l.neto > 0)
                .ToList();

            decimal baseItems = lineas.Sum(l => l.neto);
            decimal envio = Math.Max(factura.MontoEnvio, 0);
            decimal baseTotal = baseItems + envio;

            decimal descuento = Math.Max(factura.Descuento, 0);
            if (descuento > baseTotal) descuento = baseTotal;
            decimal factor = baseTotal > 0 ? (baseTotal - descuento) / baseTotal : 0;

            void Sumar(decimal tasa, decimal baseMonto)
            {
                if (baseMonto <= 0) return;
                if (tasa > 0)
                {
                    if (!r.BasePorTasa.ContainsKey(tasa))
                    {
                        r.BasePorTasa[tasa] = 0;
                        r.ISVPorTasa[tasa] = 0;
                    }
                    r.BasePorTasa[tasa] += baseMonto;
                    r.BaseGravableTotal += baseMonto;
                }
                else if (factura.EsExonerada)
                {
                    r.BaseExoneradaTotal += baseMonto;
                }
                else
                {
                    r.BaseExentaTotal += baseMonto;
                }
                r.Subtotal += baseMonto;
            }

            decimal isvLineas = 0;
            var baseLineasPorTasa = new Dictionary<decimal, decimal>();
            foreach (var (neto, tasa) in lineas)
            {
                var netoConDescuento = Math.Round(neto * factor, 2);
                Sumar(tasa, netoConDescuento);
                if (tasa > 0)
                {
                    baseLineasPorTasa[tasa] = baseLineasPorTasa.GetValueOrDefault(tasa) + netoConDescuento;
                }
            }
            foreach (var kv in baseLineasPorTasa)
            {
                isvLineas += Math.Round(kv.Value * (kv.Key / 100m), 2);
            }

            if (envio > 0)
            {
                var envioConDescuento = Math.Round(envio * factor, 2);

                // Tasa del envío: la que explica el ISV que sobra después de las líneas
                var tasasConocidas = context.Impuestos.Where(i => i.Activo).Select(i => i.Porcentaje).ToList();
                var predeterminada = context.Impuestos
                    .Where(i => i.Activo && i.EsPredeterminado)
                    .Select(i => (decimal?)i.Porcentaje)
                    .FirstOrDefault() ?? 0m;

                decimal tasaEnvio = predeterminada;
                var sobrante = factura.Impuestos - isvLineas;
                if (envioConDescuento > 0)
                {
                    if (sobrante <= 0.02m)
                    {
                        tasaEnvio = 0m;
                    }
                    else
                    {
                        var deducida = Math.Round(sobrante / envioConDescuento * 100m, 0);
                        if (deducida > 0 && tasasConocidas.Contains(deducida)) tasaEnvio = deducida;
                    }
                }

                Sumar(tasaEnvio, envioConDescuento);
            }

            foreach (var tasa in r.BasePorTasa.Keys.ToList())
            {
                var isv = Math.Round(r.BasePorTasa[tasa] * (tasa / 100m), 2);
                r.ISVPorTasa[tasa] = isv;
                r.ISVTotal += isv;
            }

            r.DescuentoAplicadoBase = descuento;
            r.Total = Math.Round(r.Subtotal + r.ISVTotal, 2);
            return r;
        }

        /// <summary>
        /// Marca una factura recién creada (desde cotización o pedido) como exonerada: copia los
        /// datos de la constancia del cliente y le quita el ISV al total y al saldo.
        /// Las líneas de detalle hay que guardarlas a 0% (Total = Subtotal).
        /// </summary>
        public static void MarcarExonerada(Factura factura, Cliente cliente)
        {
            factura.EsExonerada = true;
            factura.ExoneradoConstancia = cliente.ExoneradoConstancia;
            factura.ExoneradoOrdenCompraExenta = cliente.ExoneradoOrdenCompraExenta;
            factura.ExoneradoRegistroSag = cliente.ExoneradoRegistroSag;

            var isv = factura.Impuestos;
            factura.Impuestos = 0;
            factura.Total = Math.Max(0, factura.Total - isv);
            factura.Saldo = Math.Max(0, factura.Saldo - isv);
        }

        public static List<Impuesto> ObtenerTasasActivas(ApplicationDbContext context)
        {
            return context.Impuestos
                .Where(i => i.Activo)
                .OrderBy(i => i.Porcentaje)
                .ToList();
        }
    }

    public class ItemParaISV
    {
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public int? ImpuestoId { get; set; }
    }
}