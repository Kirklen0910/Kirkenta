using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.RRHH.Vales
{
    public class EntregarModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EntregarModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public ValeEmpleado Vale { get; set; } = new();
        public Empleado Empleado { get; set; } = new();
        public List<CuentaFinanciera> Cuentas { get; set; } = new();

        [BindProperty]
        [Required(ErrorMessage = "Debes seleccionar una cuenta")]
        public int CuentaId { get; set; }

        [BindProperty]
        public string? Notas { get; set; }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "ValesEntregar", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Vales/Index");
            }

            var vale = _context.ValesEmpleado.FirstOrDefault(v => v.Id == id);
            if (vale == null)
            {
                TempData["Error"] = "Vale no encontrado";
                return RedirectToPage("/RRHH/Vales/Index");
            }

            if (vale.Estado != "AprobadoRRHH")
            {
                TempData["Error"] = "Este vale no está listo para entregar";
                return RedirectToPage("/RRHH/Vales/Index");
            }

            Vale = vale;
            Empleado = _context.Empleados.FirstOrDefault(e => e.Id == vale.EmpleadoId) ?? new Empleado();
            Cuentas = _context.CuentasFinancieras
                .Where(c => c.Activa && c.Tipo == "Caja")
                .OrderBy(c => c.Nombre)
                .ToList();

            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "ValesEntregar", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Vales/Index");
            }

            var vale = _context.ValesEmpleado.FirstOrDefault(v => v.Id == id);
            if (vale == null || vale.Estado != "AprobadoRRHH")
            {
                TempData["Error"] = "El vale no se puede entregar";
                return RedirectToPage("/RRHH/Vales/Index");
            }

            Vale = vale;
            Empleado = _context.Empleados.FirstOrDefault(e => e.Id == vale.EmpleadoId) ?? new Empleado();
            Cuentas = _context.CuentasFinancieras
                .Where(c => c.Activa && c.Tipo == "Caja")
                .OrderBy(c => c.Nombre)
                .ToList();

            // ===== VALIDACIÓN: período contable cerrado =====
            var (periodoOk, periodoError) = CierreContableHelper.ValidarFecha(_context, DateTime.Today);
            if (!periodoOk)
            {
                ModelState.AddModelError(string.Empty, periodoError!);
            }

            if (!ModelState.IsValid) return Page();

            var cuenta = _context.CuentasFinancieras.FirstOrDefault(c => c.Id == CuentaId);
            if (cuenta == null || !cuenta.Activa || cuenta.Tipo != "Caja")
            {
                ModelState.AddModelError("CuentaId", "Selecciona una caja activa");
                return Page();
            }

            // Verificar saldo disponible
            if (cuenta.SaldoActual < vale.Monto)
            {
                ModelState.AddModelError("CuentaId",
                    $"Saldo insuficiente en '{cuenta.Nombre}' (disponible: L. {cuenta.SaldoActual:N2}, requerido: L. {vale.Monto:N2})");
                return Page();
            }

            // Todo o nada: movimiento + saldo + documento origen en una sola transacción
            using var tx = _context.Database.BeginTransaction();

            // Reclamo atómico: un vale se entrega una sola vez (doble clic / dos personas a la vez)
            if (_context.Database.IsRelational())
            {
                var reclamado = _context.ValesEmpleado
                    .Where(v => v.Id == vale.Id && v.Estado == "AprobadoRRHH")
                    .ExecuteUpdate(u => u.SetProperty(v => v.Estado, "Entregado"));
                if (reclamado == 0)
                {
                    tx.Rollback();
                    TempData["Error"] = "Este vale ya fue entregado por otra persona";
                    return RedirectToPage("/RRHH/Vales/Index");
                }
                _context.Entry(vale).Reload();
                _context.Entry(cuenta).Reload();
                if (cuenta.SaldoActual < vale.Monto)
                {
                    tx.Rollback();
                    _context.ChangeTracker.Clear();
                    TempData["Error"] = $"Saldo insuficiente en '{cuenta.Nombre}' para entregar el vale";
                    return RedirectToPage("/RRHH/Vales/Index");
                }
            }

            // Generar movimiento de egreso
            var numeroMov = NumeroDocumentoHelper.GenerarSiguiente(_context, "MovimientoFinanciero");
            var monedaDefecto = _context.Monedas.FirstOrDefault(m => m.EsPredeterminada)?.Id ?? 1;

            var categoria = _context.CategoriasFinancieras
                .FirstOrDefault(c => c.Tipo == "Egreso" && c.Nombre.Contains("Adelanto"));

            if (categoria == null)
            {
                categoria = new CategoriaFinanciera
                {
                    Tipo = "Egreso",
                    Nombre = "Adelantos/Vales a empleados",
                    Color = "#b91c1c",
                    EsSistema = true,
                    Activa = true,
                    FechaCreacion = DateTime.Now,
                    EmpresaId = EmpresaActual.Id
                };
                _context.CategoriasFinancieras.Add(categoria);
                _context.SaveChanges();
            }

            var movimiento = new MovimientoFinanciero
            {
                Numero = numeroMov,
                Tipo = "Egreso",
                Fecha = DateTime.Now,
                CuentaId = CuentaId,
                CategoriaId = categoria.Id,
                Monto = vale.Monto,
                MonedaId = monedaDefecto,
                TipoCambio = 1,
                Concepto = $"Vale {vale.Numero} — {Empleado.Nombres} {Empleado.Apellidos}",
                Referencia = vale.Numero,
                FormaPago = "Efectivo",
                Origen = "Vale",
                OrigenId = vale.Id,
                EsAutomatico = true,
                Estado = "Activo",
                FechaCreacion = DateTime.Now,
                UsuarioCreoId = currentUser?.Id,
                EmpresaId = EmpresaActual.Id
            };

            _context.MovimientosFinancieros.Add(movimiento);
            _context.SaveChanges();

            SaldoHelper.Aplicar(_context, movimiento);
            _context.SaveChanges();

            // Actualizar vale
            vale.Estado = "Entregado";
            vale.FechaEntrega = DateTime.Now;
            vale.EntregadoPorId = currentUser?.Id;
            vale.CuentaId = CuentaId;
            vale.MovimientoId = movimiento.Id;

            if (!string.IsNullOrWhiteSpace(Notas))
            {
                vale.Notas = (string.IsNullOrWhiteSpace(vale.Notas) ? "" : vale.Notas + "\n") +
                             $"[Entrega {DateTime.Now:dd/MM/yyyy HH:mm}] {Notas}";
            }

            _context.SaveChanges();
            tx.Commit();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Entregar vale",
                $"Entregó vale {vale.Numero} por L. {vale.Monto:N2} a {Empleado.Nombres} {Empleado.Apellidos}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Vale {vale.Numero} entregado correctamente. Movimiento {movimiento.Numero} registrado en Finanzas.";
            return RedirectToPage("/RRHH/Vales/Index");
        }
    }
}