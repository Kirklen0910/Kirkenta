using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Saldo a favor del cliente: se acredita cuando una devolución/cambio se deja como crédito
    /// y se descuenta cuando el cliente lo usa para pagar una compra. Ninguna función llama SaveChanges:
    /// los movimientos viajan en la misma transacción que la devolución o la venta.
    /// </summary>
    public static class SaldoFavorHelper
    {
        public const string NombreMetodo = "Saldo a favor";

        public static decimal Saldo(ApplicationDbContext context, int clienteId)
            => context.SaldoFavorMovimientos.Where(m => m.ClienteId == clienteId).Sum(m => (decimal?)m.Monto) ?? 0m;

        public static async Task<decimal> SaldoAsync(ApplicationDbContext context, int clienteId)
            => await context.SaldoFavorMovimientos.Where(m => m.ClienteId == clienteId).SumAsync(m => (decimal?)m.Monto) ?? 0m;

        public static SaldoFavorMovimiento Acreditar(ApplicationDbContext context, int clienteId, decimal monto, string concepto,
            int? devolucionId = null, int? ventaId = null, int? usuarioId = null, string tipo = "Credito")
        {
            if (monto <= 0) throw new ArgumentException("El monto a acreditar debe ser mayor a cero.", nameof(monto));
            var mov = new SaldoFavorMovimiento
            {
                ClienteId = clienteId,
                Fecha = DateTime.Now,
                Monto = Math.Round(monto, 2, MidpointRounding.AwayFromZero),
                Tipo = tipo,
                Concepto = concepto,
                DevolucionId = devolucionId,
                VentaId = ventaId,
                UsuarioId = usuarioId
            };
            context.SaldoFavorMovimientos.Add(mov);
            return mov;
        }

        public static SaldoFavorMovimiento Usar(ApplicationDbContext context, int clienteId, decimal monto, string concepto,
            int? ventaId = null, int? usuarioId = null)
        {
            if (monto <= 0) throw new ArgumentException("El monto a usar debe ser mayor a cero.", nameof(monto));
            var mov = new SaldoFavorMovimiento
            {
                ClienteId = clienteId,
                Fecha = DateTime.Now,
                Monto = -Math.Round(monto, 2, MidpointRounding.AwayFromZero),
                Tipo = "Uso",
                Concepto = concepto,
                VentaId = ventaId,
                UsuarioId = usuarioId
            };
            context.SaldoFavorMovimientos.Add(mov);
            return mov;
        }

        /// <summary>
        /// Método de pago interno "Saldo a favor". Queda inactivo para que no aparezca como opción manual en el POS:
        /// solo se usa al aplicar el saldo del cliente.
        /// </summary>
        public static MetodoPago AsegurarMetodoPago(ApplicationDbContext context)
        {
            var mp = context.MetodosPago.FirstOrDefault(m => m.Nombre == NombreMetodo);
            if (mp != null) return mp;
            mp = new MetodoPago { Nombre = NombreMetodo, Tipo = "SaldoFavor", RequiereReferencia = false, Activo = false };
            context.MetodosPago.Add(mp);
            context.SaveChanges();
            return mp;
        }
    }
}
