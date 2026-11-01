using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Aprueba una conversión de inventario (A → B). Mueve el stock por el kardex en ambos productos
    /// y traslada el costo: no genera pérdida ni ganancia, el valor del inventario no cambia.
    /// </summary>
    public static class ConversionInventarioHelper
    {
        /// <summary>Devuelve null si todo salió bien, o el mensaje de error. No llama SaveChanges si hay error.</summary>
        public static string? Aprobar(ApplicationDbContext ctx, ConversionInventario c, int usuarioId)
        {
            if (c.Estado != "Pendiente") return "Esta conversión ya fue procesada";
            if (c.CantidadOrigen <= 0 || c.CantidadDestino <= 0) return "Las cantidades deben ser mayores a 0";

            var origen = ctx.Productos.FirstOrDefault(p => p.Id == c.ProductoOrigenId);
            if (origen == null) return "El producto de origen ya no existe";
            if (origen.Stock < c.CantidadOrigen)
                return $"Stock insuficiente de '{origen.Nombre}': hay {origen.Stock:N2} y la conversión pide {c.CantidadOrigen:N2}";

            Producto? destino = null;
            if (c.ProductoDestinoId.HasValue)
            {
                if (c.ProductoDestinoId.Value == origen.Id) return "El producto de destino debe ser distinto al de origen";
                destino = ctx.Productos.FirstOrDefault(p => p.Id == c.ProductoDestinoId.Value);
                if (destino == null) return "El producto de destino ya no existe";
            }
            else if (string.IsNullOrWhiteSpace(c.NuevoProductoNombre))
            {
                return "Falta el nombre del producto nuevo";
            }

            // Costo trasladado: lo que sale de A entra a B (sin pérdida)
            var costoOrigen = origen.PrecioCompra;
            var costoTotal = Math.Round(c.CantidadOrigen * costoOrigen, 4);
            var costoDestino = Math.Round(costoTotal / c.CantidadDestino, 4);

            if (destino == null)
            {
                destino = new Producto
                {
                    SKU = NumeroDocumentoHelper.GenerarSiguiente(ctx, "Producto"),
                    Nombre = c.NuevoProductoNombre!.Trim(),
                    CategoriaId = origen.CategoriaId,
                    ImpuestoId = origen.ImpuestoId,
                    PrecioCompra = costoDestino,
                    PrecioVenta = c.NuevoProductoPrecioVenta,
                    Stock = 0,
                    UnidadMedida = string.IsNullOrWhiteSpace(c.NuevoProductoUnidad) ? "Unidad" : c.NuevoProductoUnidad!.Trim(),
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };
                ctx.Productos.Add(destino);
                ctx.SaveChanges();
                c.ProductoDestinoId = destino.Id;
            }

            // El costo de B se promedia con lo que ya tenía (si no tenía stock, queda el costo trasladado)
            var nuevoCostoDestino = InventarioHelper.CostoPromedio(destino.Stock, destino.PrecioCompra, c.CantidadDestino, costoDestino);

            InventarioHelper.Registrar(ctx, origen, -c.CantidadOrigen, "Conversión", c.Numero, c.Id, usuarioId, costoOrigen,
                $"Conversión a '{destino.Nombre}' (sin pérdida)");
            InventarioHelper.Registrar(ctx, destino, c.CantidadDestino, "Conversión", c.Numero, c.Id, usuarioId, costoDestino,
                $"Conversión desde '{origen.Nombre}'");
            destino.PrecioCompra = nuevoCostoDestino;

            c.CostoUnitarioOrigen = costoOrigen;
            c.CostoTotal = costoTotal;
            c.CostoUnitarioDestino = costoDestino;
            c.Estado = "Aprobada";
            c.UsuarioApruebaId = usuarioId;
            c.FechaAprobacion = DateTime.Now;
            return null;
        }
    }
}
