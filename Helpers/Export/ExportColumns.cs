using Kirkenta.Models;

namespace Kirkenta.Helpers.Export
{
    public static class ExportColumns
    {
        // ============================================================
        // PRODUCTOS
        // ============================================================
        public static List<ExportColumn<Producto>> Productos(
            Dictionary<int, string> categorias,
            Dictionary<int, string> impuestos)
        {
            return new List<ExportColumn<Producto>>
            {
                new("SKU", p => p.SKU ?? "", width: 15),
                new("Nombre", p => p.Nombre, width: 35),
                new("Descripción", p => p.Descripcion ?? "", width: 40),
                new("Categoría", p => p.CategoriaId.HasValue ? categorias.GetValueOrDefault(p.CategoriaId.Value, "") : "", width: 20),
                new("Impuesto", p => p.ImpuestoId.HasValue ? impuestos.GetValueOrDefault(p.ImpuestoId.Value, "") : "", width: 15),
                new("Unidad de medida", p => p.UnidadMedida ?? "", width: 18),
                new("Código de barras", p => p.CodigoBarras ?? "", width: 18),
                new("Precio compra", p => p.PrecioCompra, format: "#,##0.00", width: 15),
                new("Precio venta", p => p.PrecioVenta, format: "#,##0.00", width: 15),
                new("Precio mayorista", p => p.PrecioMayorista, format: "#,##0.00", width: 16),
                new("Stock", p => p.Stock, format: "#,##0.00", width: 12),
                new("Stock mínimo", p => p.StockMinimo, format: "#,##0.00", width: 14),
                new("Activo", p => p.Activo, width: 10),
                new("Fecha creación", p => p.FechaCreacion, format: "dd/MM/yyyy HH:mm", width: 20),
            };
        }

        // ============================================================
        // CLIENTES
        // ============================================================
        public static List<ExportColumn<Cliente>> Clientes()
        {
            return new List<ExportColumn<Cliente>>
            {
                new("Código", c => c.Codigo ?? "", width: 15),
                new("Nombre", c => c.Nombre, width: 35),
                new("Razón social", c => c.RazonSocial ?? "", width: 35),
                new("RTN", c => c.RTN ?? "", width: 18),
                new("Email", c => c.Email ?? "", width: 28),
                new("Teléfono", c => c.Telefono ?? "", width: 16),
                new("Dirección", c => c.Direccion ?? "", width: 40),
                new("Ciudad", c => c.Ciudad ?? "", width: 20),
                new("País", c => c.Pais ?? "Honduras", width: 15),
                new("Tipo cliente", c => c.TipoCliente ?? "Regular", width: 16),
                new("Días crédito", c => c.DiasCredito, width: 14),
                new("Límite crédito", c => c.LimiteCredito, format: "#,##0.00", width: 16),
                new("Notas", c => c.Notas ?? "", width: 30),
                new("Activo", c => c.Activo, width: 10),
                new("Fecha registro", c => c.FechaCreacion, format: "dd/MM/yyyy HH:mm", width: 20),
            };
        }

        // ============================================================
        // PROVEEDORES
        // ============================================================
        public static List<ExportColumn<Proveedor>> Proveedores()
        {
            return new List<ExportColumn<Proveedor>>
            {
                new("Código", p => p.Codigo ?? "", width: 15),
                new("Nombre", p => p.Nombre, width: 35),
                new("Razón social", p => p.RazonSocial ?? "", width: 35),
                new("RTN", p => p.RTN ?? "", width: 18),
                new("Contacto", p => p.Contacto ?? "", width: 25),
                new("Teléfono contacto", p => p.TelefonoContacto ?? "", width: 16),
                new("Teléfono principal", p => p.Telefono ?? "", width: 16),
                new("Email", p => p.Email ?? "", width: 28),
                new("Dirección", p => p.Direccion ?? "", width: 40),
                new("Ciudad", p => p.Ciudad ?? "", width: 20),
                new("País", p => p.Pais ?? "Honduras", width: 15),
                new("Condición pago", p => p.CondicionPago ?? "Contado", width: 16),
                new("Días crédito", p => p.DiasCredito, width: 14),
                new("Límite crédito", p => p.LimiteCredito, format: "#,##0.00", width: 16),
                new("Banco", p => p.Banco ?? "", width: 20),
                new("Cuenta bancaria", p => p.CuentaBancaria ?? "", width: 20),
                new("Activo", p => p.Activo, width: 10),
                new("Fecha creación", p => p.FechaCreacion, format: "dd/MM/yyyy HH:mm", width: 20),
            };
        }

        // ============================================================
        // CATEGORÍAS
        // ============================================================
        public static List<ExportColumn<Categoria>> Categorias()
        {
            return new List<ExportColumn<Categoria>>
            {
                new("Nombre",         c => c.Nombre,                width: 30),
                new("Descripción",    c => c.Descripcion ?? "",     width: 40),
                new("Color",          c => c.Color,                 width: 12),
                new("Activa",         c => c.Activa,                width: 10),
                new("Fecha creación", c => c.FechaCreacion, format: "dd/MM/yyyy HH:mm", width: 20),
            };
        }

        // ============================================================
        // IMPUESTOS
        // ============================================================
        public static List<ExportColumn<Impuesto>> Impuestos()
        {
            return new List<ExportColumn<Impuesto>>
            {
                new("Nombre",         i => i.Nombre,              width: 30),
                new("Porcentaje",     i => i.Porcentaje, format: "0.00", width: 14),
                new("Descripción",    i => i.Descripcion ?? "",   width: 40),
                new("Predeterminado", i => i.EsPredeterminado,    width: 15),
                new("Activo",         i => i.Activo,              width: 10),
            };
        }

        // ============================================================
        // UNIDADES DE MEDIDA
        // ============================================================
        public static List<ExportColumn<UnidadMedida>> UnidadesMedida()
        {
            return new List<ExportColumn<UnidadMedida>>
            {
                new("Nombre",         u => u.Nombre,                width: 25),
                new("Abreviatura",    u => u.Abreviatura,           width: 15),
                new("Descripción",    u => u.Descripcion ?? "",     width: 40),
                new("Activa",         u => u.Activa,                width: 10),
                new("Fecha creación", u => u.FechaCreacion, format: "dd/MM/yyyy HH:mm", width: 20),
            };
        }
    }
}