namespace Kirkenta.Models
{
    public class Permiso
    {
        public int Id { get; set; }
        public int RolId { get; set; }
        public string Modulo { get; set; } = string.Empty;
        public string? Submodulo { get; set; }
        public bool PuedeVer { get; set; } = true;
        public bool PuedeCrear { get; set; } = false;
        public bool PuedeEditar { get; set; } = false;
        public bool PuedeEliminar { get; set; } = false;
    }
}