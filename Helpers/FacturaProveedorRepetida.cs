using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Factura de proveedor repetida. Varios proveedores pueden usar el mismo número (cada uno tiene su propio
    /// rango del SAR), así que solo se compara contra las compras del MISMO proveedor (su RTN), y el número
    /// tiene que ser exactamente el mismo: «…3245» no es la misma factura que «…32456».
    /// </summary>
    public static class FacturaProveedorRepetida
    {
        public static string Marca(string numero) => $"Factura del proveedor N° {numero.Trim()}";

        /// <summary>Número de la orden que ya tiene esa factura de ese proveedor, o null.</summary>
        public static string? OrdenConLaFactura(ApplicationDbContext db, int proveedorId, string numeroFactura)
        {
            if (string.IsNullOrWhiteSpace(numeroFactura)) return null;
            var marca = Marca(numeroFactura);
            // en las notas la marca siempre va seguida de « (fecha …)» o de «.»: así no coincide un número más largo
            string conFecha = marca + " (", conPunto = marca + ".";
            return db.OrdenesCompra.AsNoTracking()
                .Where(o => o.ProveedorId == proveedorId && o.Notas != null && o.Estado != "Cancelada" && o.Estado != "Anulada"
                            && (o.Notas.Contains(conFecha) || o.Notas.Contains(conPunto)))
                .Select(o => o.Numero).FirstOrDefault();
        }
    }
}
