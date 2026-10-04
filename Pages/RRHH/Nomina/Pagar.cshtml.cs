using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Helpers.RRHH;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.RRHH.Nomina
{
    public class PagarModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PagarModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Models.Nomina Nomina { get; set; } = new();
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
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "NominaPagar", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Nomina/Index");
            }

            var nomina = _context.Nominas.FirstOrDefault(n => n.Id == id);
            if (nomina == null || nomina.Estado != "Aprobada")
            {
                TempData["Error"] = "Nómina no encontrada o no está aprobada";
                return RedirectToPage("/RRHH/Nomina/Index");
            }

            Nomina = nomina;
            Cuentas = _context.CuentasFinancieras
                .Where(c => c.Activa && (c.Tipo == "Banco" || c.Tipo == "Caja"))
                .OrderBy(c => c.Tipo).ThenBy(c => c.Nombre)
                .ToList();

            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "NominaPagar", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Nomina/Index");
            }

            var nomina = _context.Nominas.FirstOrDefault(n => n.Id == id);
            if (nomina == null || nomina.Estado != "Aprobada")
            {
                TempData["Error"] = "La nómina no se puede pagar";
                return RedirectToPage("/RRHH/Nomina/Index");
            }

            Nomina = nomina;
            Cuentas = _context.CuentasFinancieras
                .Where(c => c.Activa && (c.Tipo == "Banco" || c.Tipo == "Caja"))
                .OrderBy(c => c.Tipo).ThenBy(c => c.Nombre)
                .ToList();

            if (!ModelState.IsValid) return Page();

            var cuenta = _context.CuentasFinancieras.FirstOrDefault(c => c.Id == CuentaId);
            if (cuenta == null)
            {
                ModelState.AddModelError("CuentaId", "Cuenta no encontrada");
                return Page();
            }

            if (cuenta.SaldoActual < nomina.TotalNeto)
            {
                ModelState.AddModelError("CuentaId",
                    $"Saldo insuficiente en '{cuenta.Nombre}'. Disponible: L. {cuenta.SaldoActual:N2}, requerido: L. {nomina.TotalNeto:N2}");
                return Page();
            }

            // Generar movimiento de egreso
            var numeroMov = NumeroDocumentoHelper.GenerarSiguiente(_context, "MovimientoFinanciero");
            var monedaDefecto = _context.Monedas.FirstOrDefault(m => m.EsPredeterminada)?.Id ?? 1;

            var categoria = _context.CategoriasFinancieras
                .FirstOrDefault(c => c.Tipo == "Egreso" && c.Nombre.Contains("Nómina"));

            if (categoria == null)
            {
                categoria = new CategoriaFinanciera
                {
                    Tipo = "Egreso",
                    Nombre = "Nómina",
                    Color = "#dc2626",
                    EsSistema = true,
                    Activa = true,
                    FechaCreacion = DateTime.Now,
                    EmpresaId = 1
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
                Monto = nomina.TotalNeto,
                MonedaId = monedaDefecto,
                TipoCambio = 1,
                Concepto = $"Pago de nómina {nomina.Numero} — {nomina.PeriodoDescripcion}",
                Referencia = nomina.Numero,
                FormaPago = "Transferencia",
                Origen = "Nomina",
                OrigenId = nomina.Id,
                EsAutomatico = true,
                Estado = "Activo",
                FechaCreacion = DateTime.Now,
                UsuarioCreoId = currentUser?.Id,
                EmpresaId = 1
            };

            _context.MovimientosFinancieros.Add(movimiento);
            _context.SaveChanges();

            SaldoHelper.Aplicar(_context, movimiento);
            _context.SaveChanges();

            // Actualizar nómina
            nomina.Estado = "Pagada";
            nomina.FechaPagoReal = DateTime.Now;
            nomina.UsuarioPagaId = currentUser?.Id;
            nomina.CuentaId = CuentaId;
            nomina.MovimientoId = movimiento.Id;

            if (!string.IsNullOrWhiteSpace(Notas))
            {
                nomina.Notas = (nomina.Notas ?? "") + $"\n[Pago {DateTime.Now:dd/MM HH:mm}] {Notas}";
            }

            // ===== DESCONTAR VALES =====
            var detalles = _context.DetalleNominas.Where(d => d.NominaId == nomina.Id).ToList();
            foreach (var detalle in detalles)
            {
                if (detalle.DeduccionVales > 0)
                {
                    AplicarDescuentoVales(_context, detalle.EmpleadoId, detalle.DeduccionVales, nomina.Id);
                }
            }

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Pagar nómina",
                $"Pagó nómina {nomina.Numero} por L. {nomina.TotalNeto:N2} desde cuenta '{cuenta.Nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Nómina {nomina.Numero} pagada. Movimiento {movimiento.Numero} en Finanzas.";
            return RedirectToPage("/RRHH/Nomina/Details", new { id = nomina.Id });
        }

        private void AplicarDescuentoVales(ApplicationDbContext context, int empleadoId, decimal montoDescontar, int nominaId)
        {
            var vales = NominaHelper.ObtenerValesActivos(context, empleadoId);
            decimal restante = montoDescontar;

            foreach (var vale in vales)
            {
                if (restante <= 0) break;

                var descuento = Math.Min(vale.MontoCuota, Math.Min(vale.SaldoPendiente, restante));
                if (descuento <= 0) continue;

                vale.SaldoPendiente -= descuento;

                var cuotaNumero = context.ValesDescuentos.Count(d => d.ValeEmpleadoId == vale.Id) + 1;

                context.ValesDescuentos.Add(new ValeDescuento
                {
                    ValeEmpleadoId = vale.Id,
                    NumeroCuota = cuotaNumero,
                    Monto = descuento,
                    Fecha = DateTime.Now,
                    NominaId = nominaId,
                    Notas = $"Descontado en nómina #{nominaId}"
                });

                if (vale.SaldoPendiente <= 0)
                {
                    vale.Estado = "Descontado";
                }

                restante -= descuento;
            }
        }
    }
}