using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.RRHH.Nomina
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Models.Nomina Nomina { get; set; } = new();
        public ConfiguracionEmpresa Empresa { get; set; } = new();
        public List<DetalleView> Detalles { get; set; } = new();
        public List<PagoNominaEmpleado> Pagos { get; set; } = new();
        public MovimientoFinanciero? MovimientoTotal { get; set; }
        public Usuario? UsuarioCalcula { get; set; }
        public Usuario? UsuarioAprueba { get; set; }
        public Usuario? UsuarioPaga { get; set; }

        public bool PuedeAprobar { get; set; }
        public bool PuedePagar { get; set; }

        public decimal TotalPagado { get; set; }
        public decimal TotalPendiente { get; set; }
        public int CantidadPagados { get; set; }
        public int CantidadPendientes { get; set; }

        public class DetalleView
        {
            public int Id { get; set; }
            public string EmpleadoCodigo { get; set; } = "";
            public string EmpleadoNombre { get; set; } = "";
            public string? FotoPath { get; set; }
            public decimal SalarioBase { get; set; }
            public decimal Bonos { get; set; }
            public decimal TotalBruto { get; set; }
            public decimal DeduccionIHSS { get; set; }
            public decimal DeduccionRAP { get; set; }
            public decimal DeduccionISR { get; set; }
            public decimal DeduccionVales { get; set; }
            public decimal TotalDeducciones { get; set; }
            public decimal SalarioNeto { get; set; }
            public bool YaPagado { get; set; }
            public int? PagoId { get; set; }
            public string? PagoNumero { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "Nomina", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Nomina/Index");
            }

            var nomina = _context.Nominas.FirstOrDefault(n => n.Id == id);
            if (nomina == null)
            {
                TempData["Error"] = "Nómina no encontrada";
                return RedirectToPage("/RRHH/Nomina/Index");
            }

            Nomina = nomina;
            Empresa = _context.ConfiguracionEmpresa.FirstOrDefault() ?? new ConfiguracionEmpresa { Nombre = "Mi Empresa" };

            UsuarioCalcula = nomina.UsuarioCalculoId.HasValue
                ? _context.Usuarios.FirstOrDefault(u => u.Id == nomina.UsuarioCalculoId.Value)
                : null;
            UsuarioAprueba = nomina.UsuarioApruebaId.HasValue
                ? _context.Usuarios.FirstOrDefault(u => u.Id == nomina.UsuarioApruebaId.Value)
                : null;
            UsuarioPaga = nomina.UsuarioPagaId.HasValue
                ? _context.Usuarios.FirstOrDefault(u => u.Id == nomina.UsuarioPagaId.Value)
                : null;

            if (nomina.MovimientoId.HasValue)
            {
                MovimientoTotal = _context.MovimientosFinancieros
                    .FirstOrDefault(m => m.Id == nomina.MovimientoId.Value);
            }

            PuedeAprobar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "NominaAprobar", "editar");
            PuedePagar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "NominaPagar", "editar");

            // Pagos individuales
            Pagos = _context.PagosNominaEmpleado
                .Where(p => p.NominaId == id && p.Estado == "Pagado")
                .ToList();

            var pagosDict = Pagos.ToDictionary(p => p.DetalleNominaId, p => p);

            var empleadosDict = _context.Empleados
                .AsNoTracking()
                .ToDictionary(e => e.Id, e => new { e.Codigo, Nombre = $"{e.Nombres} {e.Apellidos}", e.FotoPath });

            var detalles = _context.DetalleNominas
                .AsNoTracking()
                .Where(d => d.NominaId == id)
                .ToList();

            Detalles = detalles.Select(d =>
            {
                var emp = empleadosDict.GetValueOrDefault(d.EmpleadoId);
                var pago = pagosDict.GetValueOrDefault(d.Id);
                return new DetalleView
                {
                    Id = d.Id,
                    EmpleadoCodigo = emp?.Codigo ?? "—",
                    EmpleadoNombre = emp?.Nombre ?? "—",
                    FotoPath = emp?.FotoPath,
                    SalarioBase = d.SalarioBase,
                    Bonos = d.BonoTransporte + d.BonoAlimentacion + d.OtrosBonos,
                    TotalBruto = d.TotalBruto,
                    DeduccionIHSS = d.DeduccionIHSS,
                    DeduccionRAP = d.DeduccionRAP,
                    DeduccionISR = d.DeduccionISR,
                    DeduccionVales = d.DeduccionVales,
                    TotalDeducciones = d.TotalDeducciones,
                    SalarioNeto = d.SalarioNeto,
                    YaPagado = pago != null,
                    PagoId = pago?.Id,
                    PagoNumero = pago?.Numero
                };
            }).ToList();

            TotalPagado = Pagos.Sum(p => p.Monto);
            TotalPendiente = Nomina.TotalNeto - TotalPagado;
            CantidadPagados = Pagos.Count;
            CantidadPendientes = Detalles.Count - CantidadPagados;

            return Page();
        }
    }
}