namespace Kirkenta.Models
{
    public class BajaInventario
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Now;
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; }
        public string TipoBaja { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        public decimal ValorUnitario { get; set; }
        public decimal ValorTotal { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public int UsuarioSolicitaId { get; set; }
        public int? UsuarioApruebaId { get; set; }
        public DateTime? FechaAprobacion { get; set; }
        public string? MotivoRechazo { get; set; }
        public bool Revertida { get; set; } = false;
        public DateTime? FechaReversion { get; set; }
        public int? UsuarioRevierteId { get; set; }
        public string? MotivoReversion { get; set; }
        public int EmpresaId { get; set; } = 1;
    }
}