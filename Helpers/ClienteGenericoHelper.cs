using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Cliente «Consumidor final»: las ventas sin cliente necesitan uno en pagos, facturas, devoluciones y envíos.
    /// Antes se usaba el Id 1 a ciegas: en una base nueva ese cliente no existía (la venta fallaba) y en una base
    /// con clientes reales las ventas de mostrador le quedaban a nombre de una persona.
    /// </summary>
    public static class ClienteGenericoHelper
    {
        public const string Codigo = "CF";
        public const string Nombre = "Consumidor final";

        /// <summary>Id del cliente genérico (lo crea la primera vez).</summary>
        public static int Id(ApplicationDbContext db)
        {
            var id = db.Clientes.AsNoTracking()
                .Where(c => c.Codigo == Codigo || c.Nombre == Nombre)
                .OrderBy(c => c.Id).Select(c => (int?)c.Id).FirstOrDefault();
            if (id.HasValue) return id.Value;

            var cliente = new Cliente { Codigo = Codigo, Nombre = Nombre, TipoCliente = "Regular" };
            db.Clientes.Add(cliente);
            db.SaveChanges();
            return cliente.Id;
        }

        /// <summary>
        /// Corrige lo que ya quedó mal: pagos, facturas, devoluciones y envíos de ventas SIN cliente que tienen
        /// el nombre de otro cliente. Solo actúa si existe alguno. Idempotente.
        /// </summary>
        public static int Reparar(ApplicationDbContext db)
        {
            if (!db.Database.IsRelational()) return 0;
            var sinCliente = db.Ventas.AsNoTracking().Where(v => v.ClienteId == null).Select(v => v.Id);
            if (!sinCliente.Any()) return 0;

            var cf = Id(db);
            var n = db.Pagos.Where(p => p.VentaId != null && sinCliente.Contains(p.VentaId.Value) && p.ClienteId != cf)
                .ExecuteUpdate(u => u.SetProperty(p => p.ClienteId, cf));
            n += db.Facturas.Where(f => f.VentaId != null && sinCliente.Contains(f.VentaId.Value) && f.ClienteId != cf)
                .ExecuteUpdate(u => u.SetProperty(f => f.ClienteId, cf));
            n += db.Devoluciones.Where(d => d.VentaId != null && sinCliente.Contains(d.VentaId.Value) && d.ClienteId != cf)
                .ExecuteUpdate(u => u.SetProperty(d => d.ClienteId, cf));
            return n;
        }
    }
}
