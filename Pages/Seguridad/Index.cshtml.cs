using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Seguridad
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class RolResumen
        {
            public string Nombre { get; set; } = "";
            public string Color { get; set; } = "#6b7280";
            public string? Descripcion { get; set; }
            public bool EsSistema { get; set; }
            public int Usuarios { get; set; }
        }

        public int Total { get; set; }
        public int Activos { get; set; }
        public int Inactivos { get; set; }
        public int Bloqueados { get; set; }
        public int SinAcceso30 { get; set; }
        public int FallosHoy { get; set; }
        public bool PuedeEditar { get; set; }
        public EstadoRespaldo Respaldo { get; set; } = new(null, null, 0, 0);
        public AvisoRespaldo? AvisoRespaldo { get; set; }

        public List<RolResumen> Roles { get; set; } = new();
        public List<Usuario> UsuariosAtencion { get; set; } = new();
        public List<ActividadUsuario> Recientes { get; set; } = new();
        public Dictionary<int, string> NombresUsuario { get; set; } = new();

        public IActionResult OnGet()
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Seguridad", "Index", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver Seguridad";
                return RedirectToPage("/Index");
            }
            PuedeEditar = PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Seguridad", "Index", "editar");
            Cargar();
            Respaldo = RespaldoEstado.Leer();
            AvisoRespaldo = RespaldoEstado.Avisar(DateTime.Now);
            return Page();
        }

        public IActionResult OnPostDesbloquear(int id)
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Seguridad", "Index", "editar"))
            {
                TempData["Error"] = "No tienes permiso para desbloquear usuarios";
                return RedirectToPage();
            }

            var u = _context.Usuarios.FirstOrDefault(x => x.Id == id);
            if (u == null)
            {
                TempData["Error"] = "Usuario no encontrado";
                return RedirectToPage();
            }

            u.IntentosFallidos = 0;
            u.BloqueadoHasta = null;
            _context.SaveChanges();

            var actual = _context.Usuarios.FirstOrDefault(x => x.Username == User.Identity!.Name);
            ActividadHelper.Registrar(_context, actual?.Id ?? 0, "Desbloqueo de usuario",
                $"Se desbloqueó al usuario {u.Username}", HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Usuario {u.Username} desbloqueado";
            return RedirectToPage();
        }

        private void Cargar()
        {
            var ahora = DateTime.Now;
            var usuarios = _context.Usuarios.ToList();

            Total = usuarios.Count;
            Activos = usuarios.Count(u => u.Activo);
            Inactivos = usuarios.Count(u => !u.Activo);
            Bloqueados = usuarios.Count(u => u.BloqueadoHasta.HasValue && u.BloqueadoHasta.Value > ahora);
            var limite = ahora.AddDays(-30);
            SinAcceso30 = usuarios.Count(u => u.Activo && (!u.UltimoAcceso.HasValue || u.UltimoAcceso.Value < limite));

            var hoy = DateTime.Today;
            FallosHoy = _context.Actividades.Count(a => a.Fecha >= hoy && a.Accion.Contains("Login fallido"));

            var roles = _context.Roles.OrderBy(r => r.Nombre).ToList();
            Roles = roles.Select(r => new RolResumen
            {
                Nombre = r.Nombre,
                Color = r.Color,
                Descripcion = r.Descripcion,
                EsSistema = r.EsSistema,
                Usuarios = usuarios.Count(u => u.Rol == r.Nombre)
            }).ToList();

            UsuariosAtencion = usuarios
                .Where(u => (u.BloqueadoHasta.HasValue && u.BloqueadoHasta.Value > ahora) || u.IntentosFallidos > 0 || u.Rol == "Pendiente")
                .OrderByDescending(u => u.BloqueadoHasta.HasValue && u.BloqueadoHasta.Value > ahora)
                .ThenByDescending(u => u.IntentosFallidos)
                .Take(20)
                .ToList();

            Recientes = _context.Actividades
                .OrderByDescending(a => a.Fecha)
                .Take(15)
                .ToList();
            NombresUsuario = usuarios.ToDictionary(u => u.Id, u => u.Username);
        }
    }
}
