using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Finanzas.Movimientos
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<MovimientoItem> Movimientos { get; set; } = new();
        public List<CuentaFinanciera> Cuentas { get; set; } = new();
        public List<CategoriaFinanciera> Categorias { get; set; } = new();

        public decimal TotalIngresos { get; set; }
        public decimal TotalEgresos { get; set; }
        public decimal Balance { get; set; }
        public int CantidadMovimientos { get; set; }

        public bool PuedeCrear { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeAnular { get; set; }

        public class MovimientoItem
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public DateTime Fecha { get; set; }
            public string Tipo { get; set; } = "";
            public string Concepto { get; set; } = "";
            public string? Referencia { get; set; }
            public decimal Monto { get; set; }
            public string CuentaNombre { get; set; } = "";
            public string? CuentaDestinoNombre { get; set; }
            public string? CategoriaNombre { get; set; }
            public string? CategoriaColor { get; set; }
            public string Estado { get; set; } = "Activo";
            public string Origen { get; set; } = "Manual";
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "Movimientos", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Index");
            }

            PuedeCrear = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "MovimientosCreate", "crear");
            PuedeEditar = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "MovimientosEdit", "editar");
            PuedeAnular = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "MovimientosAnular", "eliminar");

            var cuentas = _context.CuentasFinancieras.AsNoTracking().ToList();
            var categorias = _context.CategoriasFinancieras.AsNoTracking().ToList();

            Cuentas = cuentas;
            Categorias = categorias;

            var cuentasDict = cuentas.ToDictionary(c => c.Id, c => c.Nombre);
            var categoriasDict = categorias.ToDictionary(c => c.Id, c => new { c.Nombre, c.Color });

            var lista = _context.MovimientosFinancieros
                .AsNoTracking()
                .OrderByDescending(m => m.Fecha)
                .ThenByDescending(m => m.Id)
                .ToList();

            Movimientos = lista.Select(m => new MovimientoItem
            {
                Id = m.Id,
                Numero = m.Numero,
                Fecha = m.Fecha,
                Tipo = m.Tipo,
                Concepto = m.Concepto,
                Referencia = m.Referencia,
                Monto = m.Monto,
                CuentaNombre = cuentasDict.GetValueOrDefault(m.CuentaId, "—"),
                CuentaDestinoNombre = m.CuentaDestinoId.HasValue
                    ? cuentasDict.GetValueOrDefault(m.CuentaDestinoId.Value)
                    : null,
                CategoriaNombre = m.CategoriaId.HasValue
                    ? categoriasDict.GetValueOrDefault(m.CategoriaId.Value)?.Nombre
                    : null,
                CategoriaColor = m.CategoriaId.HasValue
                    ? categoriasDict.GetValueOrDefault(m.CategoriaId.Value)?.Color
                    : null,
                Estado = m.Estado,
                Origen = m.Origen
            }).ToList();

            var activos = lista.Where(m => m.Estado == "Activo").ToList();
            TotalIngresos = activos.Where(m => m.Tipo == "Ingreso").Sum(m => m.Monto);
            TotalEgresos = activos.Where(m => m.Tipo == "Egreso").Sum(m => m.Monto);
            Balance = TotalIngresos - TotalEgresos;
            CantidadMovimientos = activos.Count;

            return Page();
        }
    }
}