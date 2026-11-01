using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Calcula y registra el Costo de Ventas (COGS) al vender.
    /// 
    /// Se crea un MovimientoFinanciero de Egreso con categoría "Costo de ventas"
    /// (PlanCuentaId = 5.1.01). El monto es PrecioCompra × Cantidad.
    /// 
    /// La contrapartida contable (Inventario → Costo) se refleja implícitamente
    /// porque el ContabilidadHelper lee SaldoActual de CuentasFinancieras como Activo.
    /// </summary>
    public static class COGSHelper
    {
        public static (MovimientoFinanciero? mov, string? error) RegistrarCostoVenta(
            ApplicationDbContext context,
            int ventaId,
            string numeroVenta,
            List<DetalleVenta> items,
            int usuarioId)
        {
            try
            {
                if (items == null || items.Count == 0)
                    return (null, "Sin items para calcular COGS.");

                // Buscar la categoría de costo de ventas
                var categoria = context.CategoriasFinancieras
                    .FirstOrDefault(c => c.Nombre == "Costo de ventas" && c.Activa);

                if (categoria == null)
                    return (null, "No existe la categoría 'Costo de ventas'. Corré FinanzasSeeder.");

                // Calcular el total del costo
                decimal totalCosto = 0;
                foreach (var item in items)
                {
                    var producto = context.Productos.FirstOrDefault(p => p.Id == item.ProductoId);
                    if (producto == null) continue;

                    var costoUnitario = producto.PrecioCompra;
                    totalCosto += costoUnitario * item.Cantidad;
                }

                if (totalCosto <= 0)
                    return (null, "El costo calculado es 0. Revisá PrecioCompra de los productos.");

                // Cuenta destino: usar la cuenta de inventario si existe, sino la caja
                var cuenta = context.CuentasFinancieras
                    .FirstOrDefault(c => c.Codigo == "INVENTARIO" && c.Activa)
                    ?? context.CuentasFinancieras
                        .FirstOrDefault(c => c.Tipo == "Caja" && c.Activa);

                if (cuenta == null)
                    return (null, "No hay cuenta de inventario ni caja activa para registrar el COGS.");

                var monedaDefecto = context.Monedas
                    .FirstOrDefault(m => m.EsPredeterminada)?.Id ?? 1;

                var numero = NumeroDocumentoHelper.GenerarSiguiente(context, "MovimientoFinanciero");

                var mov = new MovimientoFinanciero
                {
                    Numero = numero,
                    Tipo = "Egreso",
                    Fecha = DateTime.Now,
                    CuentaId = cuenta.Id,
                    CuentaDestinoId = null,
                    CategoriaId = categoria.Id,
                    Monto = totalCosto,
                    MonedaId = monedaDefecto,
                    TipoCambio = 1,
                    Concepto = $"Costo de ventas — Venta {numeroVenta}",
                    Referencia = numeroVenta,
                    FormaPago = null,
                    Origen = "Venta",
                    OrigenId = ventaId,
                    EsAutomatico = true,
                    Estado = "Activo",
                    FechaCreacion = DateTime.Now,
                    UsuarioCreoId = usuarioId,
                    EmpresaId = EmpresaActual.Id
                };

                context.MovimientosFinancieros.Add(mov);
                context.SaveChanges();

                // Nota: NO llamamos a SaldoHelper.Aplicar porque el costo
                // no afecta el saldo real de caja/banco. Solo afecta el reporte contable.

                return (mov, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[COGSHelper] Error: {ex.Message}");
                return (null, ex.Message);
            }
        }
    }
}