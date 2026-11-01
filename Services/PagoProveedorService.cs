using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Services
{
    /// <summary>
    /// Registra un pago a proveedor contra una orden de compra: valida, descuenta el saldo de la orden
    /// de forma atomica (dos cajeros pagando a la vez no pueden pagar de mas) y, si se pide,
    /// genera el egreso en Finanzas. Todo en una sola transaccion: o se guarda completo o nada.
    /// La pagina solo valida permisos y muestra el resultado.
    /// </summary>
    public static class PagoProveedorService
    {
        public class Solicitud
        {
            public int OrdenCompraId { get; set; }
            public decimal Monto { get; set; }
            public int MetodoPagoId { get; set; }
            public string? Referencia { get; set; }
            public string? Notas { get; set; }
            public bool RegistrarEnFinanzas { get; set; } = true;
        }

        public class Resultado
        {
            public PagoProveedor? Pago { get; set; }
            public OrdenCompra? Orden { get; set; }
            /// <summary>Mensaje para el usuario; null si todo salio bien.</summary>
            public string? Error { get; set; }
            /// <summary>Campo del formulario al que pertenece el error (null = error general).</summary>
            public string? Campo { get; set; }
            public bool Ok => Error == null && Pago != null;
        }

        private static Resultado Fallo(string error, string? campo = null) =>
            new() { Error = error, Campo = campo };

        public static Resultado Registrar(ApplicationDbContext context, Solicitud s, int? usuarioId)
        {
            var orden = context.OrdenesCompra.FirstOrDefault(o => o.Id == s.OrdenCompraId);
            if (orden == null) return Fallo("Orden no encontrada", "Input.OrdenCompraId");

            if (orden.Estado != "Recibida" && orden.Estado != "RecibidaParcial")
                return Fallo($"Solo se pueden pagar órdenes en estado Recibida o Recibida Parcial (estado actual: {orden.Estado})");

            if (s.Monto <= 0) return Fallo("El monto debe ser mayor a 0", "Input.Monto");

            if (orden.Saldo <= 0) return Fallo("Esta orden ya está completamente pagada");

            if (s.Monto > orden.Saldo)
                return Fallo($"El monto no puede ser mayor al saldo pendiente (L. {orden.Saldo:N2})", "Input.Monto");

            if (!context.MetodosPago.Any(m => m.Id == s.MetodoPagoId))
                return Fallo("El método de pago no existe", "Input.MetodoPagoId");

            var (periodoOk, periodoError) = CierreContableHelper.ValidarFecha(context, DateTime.Today);
            if (!periodoOk) return Fallo(periodoError!);

            // Si quien llama ya abrió una transaccion se une a ella; si no, abre la suya.
            var propia = context.Database.CurrentTransaction == null;
            var tx = propia ? context.Database.BeginTransaction() : null;

            try
            {
                // Descuento atomico del saldo: UPDATE ... WHERE Saldo >= monto.
                // Si otra persona pago mientras tanto, no se actualiza ninguna fila y se avisa.
                if (context.Database.IsRelational())
                {
                    var monto = s.Monto;
                    var filas = context.OrdenesCompra
                        .Where(o => o.Id == orden.Id && o.Saldo >= monto)
                        .ExecuteUpdate(u => u.SetProperty(o => o.Saldo, o => o.Saldo - monto));
                    if (filas == 0)
                    {
                        tx?.Rollback();
                        context.ChangeTracker.Clear();
                        return Fallo("El saldo de la orden cambió mientras registrabas el pago. Revisa el saldo y vuelve a intentar.", "Input.Monto");
                    }
                    context.Entry(orden).Reload();
                }
                else
                {
                    orden.Saldo -= s.Monto;
                }

                if (orden.Saldo <= 0)
                {
                    orden.Saldo = 0;
                    if (orden.Estado == "Recibida") orden.Estado = "Pagada";
                }

                var numero = NumeroDocumentoHelper.GenerarSiguiente(context, "PagoProveedor");
                var pago = new PagoProveedor
                {
                    Numero = numero,
                    ProveedorId = orden.ProveedorId,
                    OrdenCompraId = orden.Id,
                    Fecha = DateTime.Now,
                    Monto = s.Monto,
                    MetodoPagoId = s.MetodoPagoId,
                    Referencia = s.Referencia,
                    // La moneda y el tipo de cambio son los de la orden: el saldo esta en esa moneda
                    MonedaId = orden.MonedaId,
                    TipoCambio = orden.TipoCambio > 0 ? orden.TipoCambio : 1,
                    Notas = s.Notas,
                    UsuarioCreoId = usuarioId,
                    EmpresaId = EmpresaActual.Id
                };
                context.PagosProveedor.Add(pago);
                context.SaveChanges();

                if (s.RegistrarEnFinanzas)
                {
                    string? errorMov = null;
                    try
                    {
                        var proveedor = context.Proveedores.FirstOrDefault(p => p.Id == orden.ProveedorId);
                        var metodoPago = context.MetodosPago.FirstOrDefault(m => m.Id == s.MetodoPagoId);

                        var (mov, error) = MovimientoAutomaticoHelper.RegistrarEgresoPagoProveedor(
                            context,
                            pago.Id,
                            pago.Numero ?? "—",
                            // Finanzas trabaja en moneda local: se convierte con el tipo de cambio de la orden
                            Math.Round(pago.Monto * pago.TipoCambio, 2),
                            usuarioId ?? 0,
                            nombreProveedor: proveedor?.Nombre ?? "—",
                            formaPago: metodoPago?.Nombre,
                            tipoMetodo: metodoPago?.Tipo);

                        if (mov == null)
                            errorMov = error ?? "No se pudo registrar el movimiento financiero (revisa la configuración de cuentas y categorías)";
                    }
                    catch (Exception ex)
                    {
                        errorMov = ex.Message;
                        Console.WriteLine($"[PagoProveedorService] Error al registrar movimiento: {ex.Message}");
                    }

                    if (errorMov != null)
                    {
                        // No se deja un pago huerfano sin su movimiento financiero
                        tx?.Rollback();
                        context.ChangeTracker.Clear();
                        return Fallo($"No se registró el pago: {errorMov} (si el pago se hizo fuera del sistema, desmarca 'Registrar en Finanzas').");
                    }
                }

                tx?.Commit();
                return new Resultado { Pago = pago, Orden = orden };
            }
            catch
            {
                tx?.Rollback();
                context.ChangeTracker.Clear();
                throw;
            }
            finally
            {
                tx?.Dispose();
            }
        }
    }
}
