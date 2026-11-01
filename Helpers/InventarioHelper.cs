using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Único punto autorizado para cambiar el stock de un producto.
    /// Actualiza Producto.Stock y deja el rastro en el kardex (MovimientoInventario).
    /// No llama SaveChanges: lo hace el llamador dentro de su transacción.
    /// </summary>
    public static class InventarioHelper
    {
        /// <summary>Aplica un cambio de stock (delta con signo) y lo registra en el kardex.</summary>
        public static MovimientoInventario Registrar(
            ApplicationDbContext context,
            Producto producto,
            decimal delta,
            string origen,
            string? documentoNumero = null,
            int? documentoId = null,
            int? usuarioId = null,
            decimal? costoUnitario = null,
            string? notas = null)
        {
            var anterior = producto.Stock;
            producto.Stock = anterior + delta;

            var mov = new MovimientoInventario
            {
                ProductoId = producto.Id,
                Fecha = DateTime.Now,
                Tipo = delta > 0 ? "Entrada" : delta < 0 ? "Salida" : "Ajuste",
                Origen = origen,
                DocumentoId = documentoId,
                DocumentoNumero = documentoNumero,
                Cantidad = delta,
                StockAnterior = anterior,
                StockPosterior = producto.Stock,
                CostoUnitario = costoUnitario ?? producto.PrecioCompra,
                UsuarioId = usuarioId,
                Notas = notas
            };
            context.MovimientosInventario.Add(mov);
            return mov;
        }

        /// <summary>Fija el stock a un valor exacto (ajuste de inventario físico).</summary>
        public static MovimientoInventario? Ajustar(
            ApplicationDbContext context,
            Producto producto,
            decimal stockNuevo,
            string origen,
            int? usuarioId,
            string? notas = null,
            string? documentoNumero = null,
            int? documentoId = null)
        {
            var delta = stockNuevo - producto.Stock;
            if (delta == 0) return null;
            return Registrar(context, producto, delta, origen, documentoNumero, documentoId, usuarioId, null, notas);
        }

        /// <summary>
        /// Costo promedio ponderado: nuevo costo al entrar 'cantidad' a 'costoEntrada'
        /// cuando ya hay 'stockActual' a 'costoActual'.
        /// </summary>
        public static decimal CostoPromedio(decimal stockActual, decimal costoActual, decimal cantidad, decimal costoEntrada)
        {
            var stockBase = Math.Max(stockActual, 0);
            var total = stockBase + cantidad;
            if (total <= 0) return costoEntrada;
            return Math.Round(((stockBase * costoActual) + (cantidad * costoEntrada)) / total, 4);
        }
    }
}
