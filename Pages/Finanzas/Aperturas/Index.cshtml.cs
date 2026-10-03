using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Finanzas.Aperturas
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<AperturaItem> Aperturas { get; set; } = new();
        public List<CuentaFinanciera> Cuentas { get; set; } = new();
        public int AperturasActivas { get; set; }
        public bool PuedeCrear { get; set; }
        public bool PuedeCerrar { get; set; }

        public class AperturaItem
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public DateTime Fecha { get; set; }
            public DateTime FechaApertura { get; set; }
            public string CuentaNombre { get; set; } = "";
            public string UsuarioNombre { get; set; } = "";
            public decimal SaldoInicial { get; set; }
            public bool Activa { get; set; }
            public int? CierreId { get; set; }
            public string? CierreNumero { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "Aperturas", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Index");
            }

            PuedeCrear = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "AperturasCreate", "crear");
            PuedeCerrar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CierresCreate", "crear");

            Cuentas = _context.CuentasFinancieras
                .AsNoTracking()
                .Where(c => c.Tipo == "Caja" && c.Activa)
                .OrderBy(c => c.Nombre)
                .ToList();

            var cuentasDict = _context.CuentasFinancieras
                .AsNoTracking()
                .ToDictionary(c => c.Id, c => c.Nombre);

            var usuariosDict = _context.Usuarios
                .AsNoTracking()
                .ToDictionary(u => u.Id, u => u.Username);

            var cierresDict = _context.CierresCaja
                .AsNoTracking()
                .ToDictionary(c => c.Id, c => c.Numero);

            var lista = _context.AperturasCaja
                .AsNoTracking()
                .OrderByDescending(a => a.Fecha)
                .ThenByDescending(a => a.Id)
                .ToList();

            Aperturas = lista.Select(a => new AperturaItem
            {
                Id = a.Id,
                Numero = a.Numero,
                Fecha = a.Fecha,
                FechaApertura = a.FechaApertura,
                CuentaNombre = cuentasDict.GetValueOrDefault(a.CuentaId, "—"),
                UsuarioNombre = usuariosDict.GetValueOrDefault(a.UsuarioAbreId, "—"),
                SaldoInicial = a.SaldoInicial,
                Activa = a.Activa,
                CierreId = a.CierreId,
                CierreNumero = a.CierreId.HasValue ? cierresDict.GetValueOrDefault(a.CierreId.Value) : null
            }).ToList();

            AperturasActivas = lista.Count(a => a.Activa);

            return Page();
        }
    }
}