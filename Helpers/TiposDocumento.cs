namespace Kirkenta.Helpers
{
    /// <summary>
    /// Catálogo ÚNICO de tipos de documento que el sistema numera con series
    /// (Configuración → Series de documentos). Cada vez que un módulo nuevo empiece a usar
    /// NumeroDocumentoHelper.GenerarSiguiente(context, "MiTipo"), hay que agregarlo aquí
    /// (y en NumeroDocumentoHelper) para que aparezca en la lista de nomenclatura.
    /// </summary>
    public static class TiposDocumento
    {
        public sealed record Tipo(string Valor, string Nombre, string Grupo);

        public static readonly IReadOnlyList<Tipo> Todos = new List<Tipo>
        {
            // Ventas
            new("Cotizacion", "Cotización", "Ventas"),
            new("Pedido", "Pedido", "Ventas"),
            new("Venta", "Venta", "Ventas"),
            new("Factura", "Factura", "Ventas"),
            new("Devolucion", "Devolución", "Ventas"),
            new("Cliente", "Cliente (código)", "Ventas"),

            // Inventario
            new("Producto", "Producto (SKU)", "Inventario"),
            new("Baja", "Baja de inventario", "Inventario"),
            new("TomaFisica", "Toma física de inventario", "Inventario"),

            // Compras
            new("Proveedor", "Proveedor (código)", "Compras"),
            new("OrdenCompra", "Orden de compra", "Compras"),
            new("PagoProveedor", "Pago a proveedor", "Compras"),
            new("DevolucionProveedor", "Devolución a proveedor", "Compras"),

            // Finanzas
            new("MovimientoFinanciero", "Movimiento financiero", "Finanzas"),
            new("AperturaCaja", "Apertura de caja", "Finanzas"),
            new("CierreCaja", "Cierre de caja", "Finanzas"),
            new("ConciliacionBancaria", "Conciliación bancaria", "Finanzas"),
            new("AsientoContable", "Asiento contable", "Finanzas"),
            new("GastoCajaChica", "Gasto de caja chica", "Finanzas"),

            // Activos y producción
            new("ActivoFijo", "Activo fijo (código)", "Activos"),
            new("OrdenProduccion", "Orden de producción", "Producción"),

            // Recursos Humanos
            new("Empleado", "Empleado (código)", "Recursos Humanos"),
            new("ValeEmpleado", "Vale a empleado", "Recursos Humanos"),
            new("Vacacion", "Solicitud de vacaciones", "Recursos Humanos"),
            new("PermisoEmpleado", "Permiso o licencia", "Recursos Humanos"),
            new("Nomina", "Nómina", "Recursos Humanos"),
            new("PagoNomina", "Pago de nómina", "Recursos Humanos"),

            // Logística
            new("Envio", "Envío", "Logística"),
            new("Ruta", "Ruta de despacho", "Logística"),
            new("Repartidor", "Repartidor (código)", "Logística")
        };

        public static bool Existe(string? valor)
            => !string.IsNullOrWhiteSpace(valor) && Todos.Any(t => t.Valor == valor);

        public static string Nombre(string? valor)
            => Todos.FirstOrDefault(t => t.Valor == valor)?.Nombre ?? (valor ?? "");

        public static string Grupo(string? valor)
            => Todos.FirstOrDefault(t => t.Valor == valor)?.Grupo ?? "Otros";

        /// <summary>Tipos agrupados por módulo, en el orden del catálogo.</summary>
        public static IEnumerable<IGrouping<string, Tipo>> PorGrupo()
            => Todos.GroupBy(t => t.Grupo);
    }
}
