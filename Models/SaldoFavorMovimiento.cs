namespace Kirkenta.Models
{
    /// <summary>
    /// Libro del saldo a favor de un cliente (crédito por devoluciones o cambios que se usa en compras futuras).
    /// Monto con signo: positivo = se le acredita, negativo = lo usó en una venta.
    /// El saldo disponible es la suma de todos sus movimientos.
    /// Tipo: Credito | Uso | Reverso
    /// </summary>
    public class SaldoFavorMovimiento
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal Monto { get; set; }
        public string Tipo { get; set; } = "Credito";
        public string? Concepto { get; set; }
        public int? DevolucionId { get; set; }
        public int? VentaId { get; set; }
        public int? UsuarioId { get; set; }
        public int EmpresaId { get; set; } = Kirkenta.Helpers.EmpresaActual.Id;
    }
}
