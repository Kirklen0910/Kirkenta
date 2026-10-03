using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Finanzas.Cierres
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<CierreItem> Cierres { get; set; } = new();
        public List<CuentaFinanciera> Cuentas { get; set; } = new();

        public decimal TotalDiferencia { get; set; }
        public int CantidadSobrantes { get; set; }
        public int CantidadFaltantes { get; set; }
        public int CantidadCuadrados { get; set; }
        public int CantidadPendientesAprobar { get; set; }

        public bool PuedeCrear { get; set; }
        public bool PuedeAprobar { get; set; }

        public class CierreItem
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public DateTime Fecha { get; set; }
            public DateTime FechaCierre { get; set; }
            public string CuentaNombre { get; set; } = "";
            public string UsuarioNombre { get; set; } = "";
            public decimal EfectivoEsperado { get; set; }
            public decimal EfectivoContado { get; set; }
            public decimal Diferencia { get; set; }
            public string Resultado { get; set; } = "";
            public string EstadoActa { get; set; } = "Cerrado";
            public decimal TotalDistribuido { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "Cierres", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Index");
            }

            PuedeCrear = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CierresCreate", "crear");
            PuedeAprobar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CierresAprobar", "editar");

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

            var lista = _context.CierresCaja
                .AsNoTracking()
                .OrderByDescending(c => c.Fecha)
                .ThenByDescending(c => c.Id)
                .ToList();

            Cierres = lista.Select(c => new CierreItem
            {
                Id = c.Id,
                Numero = c.Numero,
                Fecha = c.Fecha,
                FechaCierre = c.FechaCierre,
                CuentaNombre = cuentasDict.GetValueOrDefault(c.CuentaId, "—"),
                UsuarioNombre = usuariosDict.GetValueOrDefault(c.UsuarioCierraId, "—"),
                EfectivoEsperado = c.EfectivoEsperado,
                EfectivoContado = c.EfectivoContado,
                Diferencia = c.Diferencia,
                Resultado = c.Resultado,
                EstadoActa = c.EstadoActa,
                TotalDistribuido = c.TotalDistribuido
            }).ToList();

            TotalDiferencia = lista.Sum(c => c.Diferencia);
            CantidadSobrantes = lista.Count(c => c.Resultado == "Sobrante");
            CantidadFaltantes = lista.Count(c => c.Resultado == "Faltante");
            CantidadCuadrados = lista.Count(c => c.Resultado == "Cuadrado");
            CantidadPendientesAprobar = lista.Count(c => c.EstadoActa == "Cerrado");

            return Page();
        }
    }
}