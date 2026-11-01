using System.ComponentModel.DataAnnotations;
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Finanzas.CajaChica
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class GastoInput
        {
            public DateTime Fecha { get; set; } = DateTime.Today;
            [Range(1, int.MaxValue, ErrorMessage = "Selecciona una categoría")]
            public int CategoriaId { get; set; }
            [Required(ErrorMessage = "El concepto es obligatorio"), StringLength(300)]
            public string Concepto { get; set; } = "";
            [StringLength(100)] public string? Beneficiario { get; set; }
            [StringLength(80)] public string? Comprobante { get; set; }
            [Range(0.01, 999999999, ErrorMessage = "El monto debe ser mayor a 0")]
            public decimal Monto { get; set; }
        }

        [BindProperty(SupportsGet = true)] public string? Estado { get; set; }
        [BindProperty(SupportsGet = true)] public string? Q { get; set; }
        [BindProperty(SupportsGet = true, Name = "p")] public int Pagina { get; set; } = 1;
        public PagerInfo Pager { get; set; } = new();

        [BindProperty] public GastoInput Gasto { get; set; } = new();

        public CuentaFinanciera? Fondo { get; set; }
        public List<GastoCajaChica> Gastos { get; set; } = new();
        public List<CategoriaFinanciera> Categorias { get; set; } = new();
        public List<CuentaFinanciera> CuentasOrigen { get; set; } = new();
        public Dictionary<int, string> CategoriaNombres { get; set; } = new();
        public bool PuedeCrear { get; set; }
        public bool PuedeAprobar { get; set; }
        public decimal PendienteTotal { get; set; }
        public decimal AprobadoMes { get; set; }
        public int CantPendientes { get; set; }

        private bool Permiso(string accion) =>
            PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Finanzas", "CajaChica", accion);

        private CuentaFinanciera? ResolverFondo() =>
            _context.CuentasFinancieras.FirstOrDefault(c => c.Activa && (c.Codigo == "CAJA-CHICA" || c.Subtipo == "Chica"));

        private void Cargar()
        {
            PuedeCrear = Permiso("crear");
            PuedeAprobar = Permiso("editar");
            Fondo = ResolverFondo();

            Categorias = _context.CategoriasFinancieras.Where(c => c.Tipo == "Egreso" && c.Activa)
                .OrderBy(c => c.Nombre).ToList();
            CategoriaNombres = _context.CategoriasFinancieras.ToDictionary(c => c.Id, c => c.Nombre);
            var fondoId = Fondo?.Id ?? 0;
            CuentasOrigen = _context.CuentasFinancieras
                .Where(c => c.Activa && c.Id != fondoId && c.Subtipo != "CuentasPorCobrar")
                .OrderBy(c => c.Nombre).ToList();

            var query = _context.GastosCajaChica.AsQueryable();
            if (!string.IsNullOrWhiteSpace(Estado)) query = query.Where(g => g.Estado == Estado);
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var s = Q.Trim();
                query = query.Where(g => g.Numero.Contains(s) || g.Concepto.Contains(s)
                    || (g.Beneficiario != null && g.Beneficiario.Contains(s)) || (g.Comprobante != null && g.Comprobante.Contains(s)));
            }
            Pager = new PagerInfo(Pagina, 30, query.Count());
            Gastos = query.OrderByDescending(g => g.Fecha).ThenByDescending(g => g.Id)
                .Skip(Pager.Skip).Take(Pager.PageSize).ToList();

            var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            PendienteTotal = _context.GastosCajaChica.Where(g => g.Estado == "Pendiente").Sum(g => (decimal?)g.Monto) ?? 0;
            CantPendientes = _context.GastosCajaChica.Count(g => g.Estado == "Pendiente");
            AprobadoMes = _context.GastosCajaChica.Where(g => g.Estado == "Aprobado" && g.Fecha >= inicioMes).Sum(g => (decimal?)g.Monto) ?? 0;
        }

        public IActionResult OnGet()
        {
            if (!Permiso("ver"))
            {
                TempData["Error"] = "No tienes permiso para ver caja chica";
                return RedirectToPage("/Index");
            }
            Cargar();
            return Page();
        }

        public IActionResult OnPostSolicitar()
        {
            if (!Permiso("crear")) { TempData["Error"] = "No tienes permiso para registrar gastos"; return RedirectToPage(); }

            var fondo = ResolverFondo();
            if (fondo == null)
            {
                TempData["Error"] = "No existe una cuenta de caja chica activa (código CAJA-CHICA)";
                return RedirectToPage();
            }
            if (!_context.CategoriasFinancieras.Any(c => c.Id == Gasto.CategoriaId && c.Tipo == "Egreso"))
                ModelState.AddModelError("Gasto.CategoriaId", "Categoría no válida");

            if (Gasto.Fecha.Date > DateTime.Today)
                ModelState.AddModelError("Gasto.Fecha", "La fecha del gasto no puede ser futura");

            if (!ModelState.IsValid)
            {
                Cargar();
                return Page();
            }

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var g = new GastoCajaChica
            {
                Numero = NumeroDocumentoHelper.GenerarSiguiente(_context, "GastoCajaChica"),
                Fecha = Gasto.Fecha.Date,
                CuentaId = fondo.Id,
                CategoriaId = Gasto.CategoriaId,
                Concepto = Gasto.Concepto.Trim(),
                Beneficiario = Gasto.Beneficiario,
                Comprobante = Gasto.Comprobante,
                Monto = Gasto.Monto,
                Estado = "Pendiente",
                UsuarioSolicitaId = usuario?.Id
            };
            _context.GastosCajaChica.Add(g);
            _context.SaveChanges();

            ActividadHelper.Registrar(_context, usuario?.Id ?? 0, "Gasto de caja chica",
                $"Registró el gasto {g.Numero} por L. {g.Monto:N2}: {g.Concepto}", HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = $"Gasto {g.Numero} registrado, pendiente de aprobación";
            return RedirectToPage();
        }

        public IActionResult OnPostAprobar(int id)
        {
            if (!Permiso("editar")) { TempData["Error"] = "No tienes permiso para aprobar gastos"; return RedirectToPage(); }

            var g = _context.GastosCajaChica.FirstOrDefault(x => x.Id == id);
            if (g == null || g.Estado != "Pendiente") { TempData["Error"] = "El gasto no está pendiente"; return RedirectToPage(); }

            var fondo = _context.CuentasFinancieras.FirstOrDefault(c => c.Id == g.CuentaId);
            if (fondo == null) { TempData["Error"] = "La cuenta de caja chica ya no existe"; return RedirectToPage(); }
            if (fondo.SaldoActual < g.Monto)
            {
                TempData["Error"] = $"El fondo de caja chica (L. {fondo.SaldoActual:N2}) no alcanza para este gasto. Haz una reposición primero.";
                return RedirectToPage();
            }

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);

            // Control interno: quien solicita el gasto no lo aprueba (salvo el administrador)
            if (usuario != null && g.UsuarioSolicitaId == usuario.Id && usuario.Rol != "Admin")
            {
                TempData["Error"] = "Otra persona debe aprobar este gasto: no puedes aprobar el que tú registraste.";
                return RedirectToPage();
            }

            using var tx = _context.Database.BeginTransaction();

            // Reclamo atómico: si dos personas aprueban el mismo gasto a la vez, solo una lo descuenta del fondo
            if (_context.Database.IsRelational())
            {
                var filas = _context.GastosCajaChica
                    .Where(x => x.Id == id && x.Estado == "Pendiente")
                    .ExecuteUpdate(u => u.SetProperty(x => x.Estado, "Aprobado"));
                if (filas == 0)
                {
                    tx.Rollback();
                    TempData["Error"] = "El gasto ya fue resuelto por otra persona";
                    return RedirectToPage();
                }
                _context.Entry(g).Reload();
                // Saldo fresco del fondo, ya con el gasto reclamado
                _context.Entry(fondo).Reload();
                if (fondo.SaldoActual < g.Monto)
                {
                    tx.Rollback();
                    _context.ChangeTracker.Clear();
                    TempData["Error"] = $"El fondo de caja chica (L. {fondo.SaldoActual:N2}) no alcanza para este gasto. Haz una reposición primero.";
                    return RedirectToPage();
                }
            }

            var (mov, error) = MovimientoManualHelper.Crear(_context, "Egreso", g.Fecha, g.CuentaId, null, g.CategoriaId, g.Monto,
                $"Caja chica {g.Numero}: {g.Concepto}", g.Comprobante, "CajaChica", g.Id, usuario?.Id);
            if (mov == null)
            {
                tx.Rollback();
                _context.ChangeTracker.Clear();
                TempData["Error"] = $"No se pudo aprobar: {error}";
                return RedirectToPage();
            }

            g.Estado = "Aprobado";
            g.MovimientoFinancieroId = mov.Id;
            g.UsuarioApruebaId = usuario?.Id;
            g.FechaResolucion = DateTime.Now;
            _context.SaveChanges();
            tx.Commit();

            ActividadHelper.Registrar(_context, usuario?.Id ?? 0, "Aprobar gasto de caja chica",
                $"Aprobó {g.Numero} por L. {g.Monto:N2}", HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = $"Gasto {g.Numero} aprobado y descontado del fondo";
            return RedirectToPage();
        }

        public IActionResult OnPostRechazar(int id, string? motivo)
        {
            if (!Permiso("editar")) { TempData["Error"] = "No tienes permiso para rechazar gastos"; return RedirectToPage(); }
            if (string.IsNullOrWhiteSpace(motivo)) { TempData["Error"] = "Indica el motivo del rechazo"; return RedirectToPage(); }

            var g = _context.GastosCajaChica.FirstOrDefault(x => x.Id == id);
            if (g == null || g.Estado != "Pendiente") { TempData["Error"] = "El gasto no está pendiente"; return RedirectToPage(); }

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            if (_context.Database.IsRelational())
            {
                var filas = _context.GastosCajaChica
                    .Where(x => x.Id == id && x.Estado == "Pendiente")
                    .ExecuteUpdate(u => u.SetProperty(x => x.Estado, "Rechazado"));
                if (filas == 0) { TempData["Error"] = "El gasto ya fue resuelto por otra persona"; return RedirectToPage(); }
                _context.Entry(g).Reload();
            }
            g.Estado = "Rechazado";
            g.MotivoRechazo = motivo.Trim();
            g.UsuarioApruebaId = usuario?.Id;
            g.FechaResolucion = DateTime.Now;
            _context.SaveChanges();

            ActividadHelper.Registrar(_context, usuario?.Id ?? 0, "Rechazar gasto de caja chica",
                $"Rechazó {g.Numero}: {g.MotivoRechazo}", HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = $"Gasto {g.Numero} rechazado";
            return RedirectToPage();
        }

        public IActionResult OnPostReponer(int cuentaOrigenId, decimal monto)
        {
            if (!Permiso("editar")) { TempData["Error"] = "No tienes permiso para reponer el fondo"; return RedirectToPage(); }
            if (monto <= 0) { TempData["Error"] = "El monto de la reposición debe ser mayor a 0"; return RedirectToPage(); }

            var fondo = ResolverFondo();
            if (fondo == null) { TempData["Error"] = "No existe una cuenta de caja chica activa"; return RedirectToPage(); }

            var origen = _context.CuentasFinancieras.FirstOrDefault(c => c.Id == cuentaOrigenId && c.Activa);
            if (origen == null || origen.Id == fondo.Id) { TempData["Error"] = "Cuenta de origen no válida"; return RedirectToPage(); }
            if (origen.SaldoActual < monto)
            {
                TempData["Error"] = $"La cuenta '{origen.Nombre}' solo tiene L. {origen.SaldoActual:N2}";
                return RedirectToPage();
            }

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            using var tx = _context.Database.BeginTransaction();
            if (_context.Database.IsRelational())
            {
                _context.Entry(origen).Reload();
                if (origen.SaldoActual < monto)
                {
                    tx.Rollback();
                    TempData["Error"] = $"La cuenta '{origen.Nombre}' solo tiene L. {origen.SaldoActual:N2}";
                    return RedirectToPage();
                }
            }
            var (mov, error) = MovimientoManualHelper.Crear(_context, "Transferencia", DateTime.Now, origen.Id, fondo.Id, null, monto,
                $"Reposición de caja chica desde {origen.Nombre}", null, "CajaChica", null, usuario?.Id);
            if (mov == null)
            {
                tx.Rollback();
                _context.ChangeTracker.Clear();
                TempData["Error"] = $"No se pudo reponer: {error}";
                return RedirectToPage();
            }
            tx.Commit();

            ActividadHelper.Registrar(_context, usuario?.Id ?? 0, "Reposición de caja chica",
                $"Repuso L. {monto:N2} desde {origen.Nombre}", HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = $"Fondo repuesto con L. {monto:N2}";
            return RedirectToPage();
        }
    }
}
