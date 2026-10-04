using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Finanzas.CierresContables
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
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

        public class InputModel
        {
            [Required]
            public int Anio { get; set; }

            [Required]
            [Range(1, 12)]
            public int Mes { get; set; }

            [StringLength(500)]
            public string? Notas { get; set; }

            [Required(ErrorMessage = "Debes confirmar que entiendes el impacto")]
            public bool Confirmado { get; set; }
        }

        public IActionResult OnGet(int anio, int mes)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CierresContablesCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/CierresContables/Index");
            }

            if (mes < 1 || mes > 12)
            {
                TempData["Error"] = "Mes inválido";
                return RedirectToPage("/Finanzas/CierresContables/Index");
            }

            var estado = CierreContableHelper.Obtener(_context, anio, mes);
            if (estado != null && estado.Estado == "Cerrado")
            {
                TempData["Error"] = $"El período {estado.PeriodoTexto} ya está cerrado";
                return RedirectToPage("/Finanzas/CierresContables/Index");
            }

            Input.Anio = anio;
            Input.Mes = mes;
            CargarDatos(anio, mes);

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CierresContablesCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/CierresContables/Index");
            }

            CargarDatos(Input.Anio, Input.Mes);

            if (!Input.Confirmado)
            {
                ModelState.AddModelError("Input.Confirmado", "Debes confirmar que entiendes el impacto del cierre");
            }

            if (!ModelState.IsValid) return Page();

            var (ok, error, cierre) = CierreContableHelper.Cerrar(
                _context, Input.Anio, Input.Mes, currentUser!.Id, Input.Notas);

            if (!ok)
            {
                ModelState.AddModelError(string.Empty, error ?? "Error al cerrar el período");
                return Page();
            }

            ActividadHelper.Registrar(
                _context,
                currentUser.Id,
                "Cerrar período contable",
                $"Cerró el período {cierre!.PeriodoTexto}. " +
                $"Movimientos: {cierre.CantidadMovimientos}, Ingresos: L. {cierre.TotalIngresos:N2}, " +
                $"Egresos: L. {cierre.TotalEgresos:N2}, Balance: L. {cierre.Balance:N2}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Período {cierre.PeriodoTexto} cerrado correctamente";
            return RedirectToPage("/Finanzas/CierresContables/Index", new { anio = Input.Anio });
        }

        private void CargarDatos(int anio, int mes)
        {
            var cultura = System.Globalization.CultureInfo.GetCultureInfo("es-HN");
            PeriodoTexto = $"{cultura.DateTimeFormat.GetMonthName(mes)} {anio}";

            var inicio = new DateTime(anio, mes, 1);
            var fin = inicio.AddMonths(1);

            var movimientos = _context.MovimientosFinancieros
                .Where(m => m.Estado == "Activo" && m.Fecha >= inicio && m.Fecha < fin)
                .ToList();

            TotalIngresos = movimientos.Where(m => m.Tipo == "Ingreso").Sum(m => m.Monto);
            TotalEgresos = movimientos.Where(m => m.Tipo == "Egreso").Sum(m => m.Monto);
            Balance = TotalIngresos - TotalEgresos;
            CantidadMovimientos = movimientos.Count;
        }
    }
}