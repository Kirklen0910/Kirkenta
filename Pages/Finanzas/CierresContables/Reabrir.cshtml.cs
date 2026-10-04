using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Finanzas.CierresContables
{
    public class ReabrirModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ReabrirModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string PeriodoTexto { get; set; } = "";
        public decimal TotalIngresos { get; set; }
        public decimal TotalEgresos { get; set; }
        public decimal Balance { get; set; }
        public int CantidadMovimientos { get; set; }
        public DateTime? FechaCierre { get; set; }
        public string? UsuarioCierre { get; set; }

        public class InputModel
        {
            [Required]
            public int Anio { get; set; }

            [Required]
            [Range(1, 12)]
            public int Mes { get; set; }

            [Required(ErrorMessage = "Debes indicar el motivo de la reapertura")]
            [StringLength(500, MinimumLength = 10, ErrorMessage = "El motivo debe tener al menos 10 caracteres")]
            public string Motivo { get; set; } = "";

            [Required(ErrorMessage = "Debes ingresar tu contraseña para confirmar")]
            public string Password { get; set; } = "";
        }

        public IActionResult OnGet(int anio, int mes)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CierresContablesReabrir", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/CierresContables/Index");
            }

            if (mes < 1 || mes > 12)
            {
                TempData["Error"] = "Mes inválido";
                return RedirectToPage("/Finanzas/CierresContables/Index");
            }

            var cierre = CierreContableHelper.Obtener(_context, anio, mes);
            if (cierre == null || cierre.Estado != "Cerrado")
            {
                TempData["Error"] = "El período no está cerrado";
                return RedirectToPage("/Finanzas/CierresContables/Index");
            }

            Input.Anio = anio;
            Input.Mes = mes;
            CargarDatos(cierre);

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CierresContablesReabrir", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/CierresContables/Index");
            }

            var cierre = CierreContableHelper.Obtener(_context, Input.Anio, Input.Mes);
            if (cierre == null || cierre.Estado != "Cerrado")
            {
                TempData["Error"] = "El período no está cerrado";
                return RedirectToPage("/Finanzas/CierresContables/Index");
            }

            CargarDatos(cierre);

            // Validar contraseña
            if (currentUser == null || !BCrypt.Net.BCrypt.Verify(Input.Password, currentUser.PasswordHash))
            {
                ModelState.AddModelError("Input.Password", "Contraseña incorrecta");
            }

            if (!ModelState.IsValid) return Page();

            var (ok, error) = CierreContableHelper.Reabrir(
                _context, Input.Anio, Input.Mes, currentUser!.Id, Input.Motivo);

            if (!ok)
            {
                ModelState.AddModelError(string.Empty, error ?? "Error al reabrir el período");
                return Page();
            }

            ActividadHelper.Registrar(
                _context,
                currentUser.Id,
                "Reabrir período contable",
                $"Reabrió el período {cierre.PeriodoTexto}. Motivo: {Input.Motivo}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Período {cierre.PeriodoTexto} reabierto correctamente";
            return RedirectToPage("/Finanzas/CierresContables/Index", new { anio = Input.Anio });
        }

        private void CargarDatos(Models.CierreContable cierre)
        {
            PeriodoTexto = cierre.PeriodoTexto;
            TotalIngresos = cierre.TotalIngresos;
            TotalEgresos = cierre.TotalEgresos;
            Balance = cierre.Balance;
            CantidadMovimientos = cierre.CantidadMovimientos;
            FechaCierre = cierre.FechaCierre;

            UsuarioCierre = cierre.UsuarioCierreId.HasValue
                ? _context.Usuarios.FirstOrDefault(u => u.Id == cierre.UsuarioCierreId.Value)?.Username
                : null;
        }
    }
}