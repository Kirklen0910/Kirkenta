using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Helpers.RRHH;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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

            // ===== VALIDACIÓN: período contable cerrado =====
            var (periodoOk, periodoError) = CierreContableHelper.ValidarFecha(_context, DateTime.Today);
            if (!periodoOk)
            {
                ModelState.AddModelError(string.Empty, periodoError!);
            }

            if (!ModelState.IsValid) return Page();

            var cuenta = _context.CuentasFinancieras.FirstOrDefault(c => c.Id == CuentaId);
            if (cuenta == null)
            {
                ModelState.AddModelError("CuentaId", "Cuenta no encontrada");
                return Page();
            }

            // Si ya se pagó a algunos empleados de forma individual, aquí solo se paga el resto
            // (antes se pagaba la nómina completa y esos empleados cobraban dos veces).
            var yaPagadosIds = _context.PagosNominaEmpleado
                .Where(p => p.NominaId == nomina.Id && p.Estado == "Pagado")
                .Select(p => p.DetalleNominaId)
                .ToHashSet();
            var detallesPendientes = _context.DetalleNominas
                .Where(d => d.NominaId == nomina.Id)
                .ToList()
                .Where(d => !yaPagadosIds.Contains(d.Id))
                .ToList();
            var montoPagar = detallesPendientes.Sum(d => d.SalarioNeto);

            if (detallesPendientes.Count == 0 || montoPagar <= 0)
            {
                ModelState.AddModelError(string.Empty, "Todos los empleados de esta nómina ya fueron pagados.");
                return Page();
            }

            if (cuenta.SaldoActual < montoPagar)
            {
                ModelState.AddModelError("CuentaId",
                    $"Saldo insuficiente en '{cuenta.Nombre}'. Disponible: L. {cuenta.SaldoActual:N2}, requerido: L. {montoPagar:N2}");
                return Page();
            }

            // Todo o nada: movimiento + saldo + documento origen en una sola transacción
            using var tx = _context.Database.BeginTransaction();

            // Bloqueo de la nómina y revalidación: dos personas pagando la misma nómina (o un pago total y uno
            // individual a la vez) ya no pueden pagar dos veces a los mismos empleados.
            if (_context.Database.IsRelational())
            {
                _context.Nominas.Where(n => n.Id == nomina.Id)
                    .ExecuteUpdate(u => u.SetProperty(n => n.Estado, n => n.Estado));
                var estadoActual = _context.Nominas.AsNoTracking().Where(n => n.Id == nomina.Id).Select(n => n.Estado).First();
                var pagadosAhora = _context.PagosNominaEmpleado.Count(p => p.NominaId == nomina.Id && p.Estado == "Pagado");
                if (estadoActual != "Aprobada" || pagadosAhora != yaPagadosIds.Count)
                {
                    tx.Rollback();
                    _context.ChangeTracker.Clear();
                    TempData["Error"] = "La nómina cambió mientras la pagabas (otra persona registró pagos). Revísala e inténtalo de nuevo.";
                    return RedirectToPage("/RRHH/Nomina/Details", new { id });
                }
            }

            // Generar movimiento de egreso
            var numeroMov = NumeroDocumentoHelper.GenerarSiguiente(_context, "MovimientoFinanciero");
            var monedaDefecto = _context.Monedas.FirstOrDefault(m => m.EsPredeterminada)?.Id ?? 1;

            var categoria = NominaHelper.EsDecimo(nomina.Tipo)
                ? MovimientoManualHelper.ObtenerOCrearCategoria(_context, "Egreso", NominaHelper.NombreTipo(nomina.Tipo), "6.2.02", "#dc2626")
                : _context.CategoriasFinancieras.FirstOrDefault(c => c.Tipo == "Egreso" && c.Nombre.Contains("Nómina"));

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
                Monto = montoPagar,
                MonedaId = monedaDefecto,
                TipoCambio = 1,
                Concepto = $"Pago de nómina {nomina.Numero} — {nomina.PeriodoDescripcion}",
                Referencia = nomina.Numero,
                FormaPago = cuenta.Tipo == "Caja" ? "Efectivo" : "Transferencia",
                Origen = "Nomina",
                OrigenId = nomina.Id,
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

            // Un registro de pago por empleado (igual que el pago individual), todos ligados al mismo movimiento
            foreach (var detalle in detallesPendientes)
            {
                _context.PagosNominaEmpleado.Add(new PagoNominaEmpleado
                {
                    Numero = NumeroDocumentoHelper.GenerarSiguiente(_context, "PagoNomina"),
                    NominaId = nomina.Id,
                    EmpleadoId = detalle.EmpleadoId,
                    DetalleNominaId = detalle.Id,
                    Monto = detalle.SalarioNeto,
                    MetodoPago = movimiento.FormaPago ?? "Transferencia",
                    Referencia = nomina.Numero,
                    CuentaId = CuentaId,
                    MovimientoId = movimiento.Id,
                    FechaPago = DateTime.Now,
                    UsuarioPagoId = currentUser?.Id,
                    Estado = "Pagado",
                    Notas = Notas,
                    EmpresaId = EmpresaActual.Id
                });
                _context.SaveChanges();
            }

            // El neto ya salió: lo demás del gasto (retenciones, vales y aportes) entra como deuda, sin mover dinero
            foreach (var pagoEmp in _context.PagosNominaEmpleado.Where(p => p.NominaId == nomina.Id && p.MovimientoId == movimiento.Id).ToList())
            {
                var det = detallesPendientes.First(d => d.Id == pagoEmp.DetalleNominaId);
                var emp = _context.Empleados.FirstOrDefault(e => e.Id == det.EmpleadoId);
                var errorRet = RetencionesHelper.RegistrarDeNomina(_context, pagoEmp, det, nomina.Numero, $"{emp?.Nombres} {emp?.Apellidos}", currentUser?.Id);
                if (errorRet != null)
                {
                    tx.Rollback();
                    _context.ChangeTracker.Clear();
                    TempData["Error"] = "No se pagó la nómina: " + errorRet;
                    return RedirectToPage("/RRHH/Nomina/Details", new { id });
                }
            }

            // DESCONTAR VALES
            foreach (var detalle in detallesPendientes)
            {
                if (detalle.DeduccionVales > 0)
                {
                    AplicarDescuentoVales(_context, detalle.EmpleadoId, detalle.DeduccionVales, nomina.Id);
                }
            }

            _context.SaveChanges();
            tx.Commit();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Pagar nómina",
                $"Pagó nómina {nomina.Numero} por L. {montoPagar:N2} desde cuenta '{cuenta.Nombre}'",
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

                var descuento = Math.Min(NominaHelper.CuotaVale(vale), restante);
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