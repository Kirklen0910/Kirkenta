namespace Kirkenta.Models
{
    public class ActividadUsuario
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string? Detalle { get; set; }
        public string? Ip { get; set; }
        public string? UserAgent { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}