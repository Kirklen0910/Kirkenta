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
    public class PagarEmpleadoModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PagarEmpleadoModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Models.Nomina Nomina { get; set; } = new();
        public List<DetallePagoItem> Detalles { get; set; } = new();
        public List<CuentaFinanciera> Cuentas { get; set; } = new();

        [BindProperty]
        public List<int> DetalleIdsSeleccionados { get; set; } = new();

        [BindProperty]
        [Required(ErrorMessage = "Debes seleccionar un método de pago")]
        public string MetodoPago { get; set; } = "Transferencia";

        [BindProperty]
        public string? Referencia { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Debes seleccionar una cuenta")]
        public int CuentaId { get; set; }

        [BindProperty]
        public string? Notas { get; set; }

        public class DetallePagoItem
        {
            public int Id { get; set; }
            public int EmpleadoId { get; set; }
            public string EmpleadoCodigo { get; set; } = "";
            public string EmpleadoNombre { get; set; } = "";
            public string? FotoPath { get; set; }
            public decimal TotalBruto { get; set; }
            public decimal TotalDeducciones { get; set; }
            public decimal SalarioNeto { get; set; }
            public bool YaPagado { get; set; }
        }

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

            CargarDatos(nomina);
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

            CargarDatos(nomina);

            if (DetalleIdsSeleccionados == null || DetalleIdsSeleccionados.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Debes seleccionar al menos un empleado");
                return Page();
            }

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

            var detallesAPagar = _context.DetalleNominas
                .Where(d => d.NominaId == nomina.Id && DetalleIdsSeleccionados.Contains(d.Id))
                .ToList();

            var totalPagar = detallesAPagar.Sum(d => d.SalarioNeto);

            if (cuenta.SaldoActual < totalPagar)
            {
                ModelState.AddModelError("CuentaId",
                    $"Saldo insuficiente. Disponible: L. {cuenta.SaldoActual:N2}, requerido: L. {totalPagar:N2}");
                return Page();
            }

            var categoria = ObtenerCategoriaNomina(_context);

            int pagosRealizados = 0;

            foreach (var detalle in detallesAPagar)
            {
                var yaPagado = _context.PagosNominaEmpleado
                    .Any(p => p.DetalleNominaId == detalle.Id && p.Estado == "Pagado");

                if (yaPagado) continue;

                var numeroMov = NumeroDocumentoHelper.GenerarSiguiente(_context, "MovimientoFinanciero");
                var monedaDefecto = _context.Monedas.FirstOrDefault(m => m.EsPredeterminada)?.Id ?? 1;
                var empleado = _context.Empleados.FirstOrDefault(e => e.Id == detalle.EmpleadoId);

                var movimiento = new MovimientoFinanciero
                {
                    Numero = numeroMov,
                    Tipo = "Egreso",
                    Fecha = DateTime.Now,
                    CuentaId = CuentaId,
                    CategoriaId = categoria.Id,
                    Monto = detalle.SalarioNeto,
                    MonedaId = monedaDefecto,
                    TipoCambio = 1,
                    Concepto = $"Nómina {nomina.Numero} — {empleado?.Nombres} {empleado?.Apellidos}",
                    Referencia = Referencia ?? nomina.Numero,
                    FormaPago = MetodoPago,
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

                var numeroPago = NumeroDocumentoHelper.GenerarSiguiente(_context, "PagoNomina");
                var pago = new PagoNominaEmpleado
                {
                    Numero = numeroPago,
                    NominaId = nomina.Id,
                    EmpleadoId = detalle.EmpleadoId,
                    DetalleNominaId = detalle.Id,
                    Monto = detalle.SalarioNeto,
                    MetodoPago = MetodoPago,
                    Referencia = Referencia,
                    CuentaId = CuentaId,
                    MovimientoId = movimiento.Id,
                    FechaPago = DateTime.Now,
                    UsuarioPagoId = currentUser?.Id,
                    Estado = "Pagado",
                    Notas = Notas,
                    EmpresaId = 1
                };

                _context.PagosNominaEmpleado.Add(pago);

                if (detalle.DeduccionVales > 0)
                {
                    AplicarDescuentoVales(_context, detalle.EmpleadoId, detalle.DeduccionVales, nomina.Id);
                }

                pagosRealizados++;
            }

            _context.SaveChanges();

            var totalDetalles = _context.DetalleNominas.Count(d => d.NominaId == nomina.Id);
            var totalPagados = _context.PagosNominaEmpleado
                .Count(p => p.NominaId == nomina.Id && p.Estado == "Pagado");

            if (totalPagados >= totalDetalles)
            {
                nomina.Estado = "Pagada";
                nomina.FechaPagoReal = DateTime.Now;
                nomina.UsuarioPagaId = currentUser?.Id;
                nomina.CuentaId = CuentaId;
                _context.SaveChanges();
            }

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Pagar nómina a empleados",
                $"Pagó {pagosRealizados} empleado(s) de la nómina {nomina.Numero}. Total: L. {totalPagar:N2}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Se pagaron {pagosRealizados} empleado(s). Total: L. {totalPagar:N2}";
            return RedirectToPage("/RRHH/Nomina/Details", new { id = nomina.Id });
        }

        private void CargarDatos(Models.Nomina nomina)
        {
            Nomina = nomina;

            var empleadosDict = _context.Empleados
                .AsNoTracking()
                .ToDictionary(e => e.Id, e => new { e.Codigo, Nombre = $"{e.Nombres} {e.Apellidos}", e.FotoPath });

            var pagosExistentes = _context.PagosNominaEmpleado
                .Where(p => p.NominaId == nomina.Id && p.Estado == "Pagado")
                .Select(p => p.DetalleNominaId)
                .ToHashSet();

            var detalles = _context.DetalleNominas
                .Where(d => d.NominaId == nomina.Id)
                .ToList();

            Detalles = detalles.Select(d =>
            {
                var emp = empleadosDict.GetValueOrDefault(d.EmpleadoId);
                return new DetallePagoItem
                {
                    Id = d.Id,
                    EmpleadoId = d.EmpleadoId,
                    EmpleadoCodigo = emp?.Codigo ?? "—",
                    EmpleadoNombre = emp?.Nombre ?? "—",
                    FotoPath = emp?.FotoPath,
                    TotalBruto = d.TotalBruto,
                    TotalDeducciones = d.TotalDeducciones,
                    SalarioNeto = d.SalarioNeto,
                    YaPagado = pagosExistentes.Contains(d.Id)
                };
            }).ToList();

            Cuentas = _context.CuentasFinancieras
                .Where(c => c.Activa && (c.Tipo == "Banco" || c.Tipo == "Caja"))
                .OrderBy(c => c.Tipo).ThenBy(c => c.Nombre)
                .ToList();
        }

        private static CategoriaFinanciera ObtenerCategoriaNomina(ApplicationDbContext context)
        {
            var categoria = context.CategoriasFinancieras
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
                context.CategoriasFinancieras.Add(categoria);
                context.SaveChanges();
            }

            return categoria;
        }

        private static void AplicarDescuentoVales(ApplicationDbContext context, int empleadoId, decimal montoDescontar, int nominaId)
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