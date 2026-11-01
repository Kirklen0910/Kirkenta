namespace Kirkenta.Models
{
    /// <summary>
    /// Conversión / fraccionamiento de inventario: se saca producto del artículo A (p. ej. 1 lance de tubo
    /// o 1 bolsa de cemento) y entra como producto B (por pie, por libra...). No es una pérdida:
    /// el costo de lo que sale de A pasa íntegro a B.
    /// </summary>
    public class ConversionInventario
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Now;

        public int ProductoOrigenId { get; set; }
        public decimal CantidadOrigen { get; set; }

        /// <summary>Producto existente que recibe lo convertido. Si es null se crea uno nuevo al aprobar.</summary>
        public int? ProductoDestinoId { get; set; }
        public string? NuevoProductoNombre { get; set; }
        public string? NuevoProductoUnidad { get; set; }
        public decimal NuevoProductoPrecioVenta { get; set; }

        public decimal CantidadDestino { get; set; }

        /// <summary>Se fijan al aprobar con el costo vigente del producto de origen.</summary>
        public decimal CostoUnitarioOrigen { get; set; }
        public decimal CostoTotal { get; set; }
        public decimal CostoUnitarioDestino { get; set; }

        public string Motivo { get; set; } = string.Empty;
        public string Estado { get; set; } = "Pendiente";
        public int UsuarioSolicitaId { get; set; }
        public int? UsuarioApruebaId { get; set; }
        public DateTime? FechaAprobacion { get; set; }
        public string? MotivoRechazo { get; set; }
        public int EmpresaId { get; set; } = Kirkenta.Helpers.EmpresaActual.Id;
    }
}
