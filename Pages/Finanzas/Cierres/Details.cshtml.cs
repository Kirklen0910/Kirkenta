using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Finanzas.Cierres
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DetailsModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public CierreCaja Cierre { get; set; } = new();
        public CuentaFinanciera Cuenta { get; set; } = new();
        public Usuario UsuarioCierra { get; set; } = new();
        public Usuario? UsuarioAprueba { get; set; }
        public AperturaCaja? Apertura { get; set; }
        public MovimientoFinanciero? MovimientoAjuste { get; set; }
        public ConfiguracionEmpresa Empresa { get; set; } = new();
        public List<MovimientoResumen> MovimientosDia { get; set; } = new();
        public List<DistribucionView> Distribuciones { get; set; } = new();
        public List<AdjuntoCierre> Adjuntos { get; set; } = new();
        /// <summary>Efectivo de este cierre que todavía no tiene su depósito registrado.</summary>
        public List<EfectivoPorDepositarHelper.Pendiente> PorDepositar { get; set; } = new();
        public List<CuentaFinanciera> Bancos { get; set; } = new();
        public List<CierreCajaConteo> Conteos { get; set; } = new();
        public InformeMetodosPago Informe { get; set; } = new();

        public bool PuedeAprobar { get; set; }
        public bool PuedeSubirAdjunto { get; set; }

        public class MovimientoResumen
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public DateTime Fecha { get; set; }
            public string Tipo { get; set; } = "";
            public string Concepto { get; set; } = "";
            public string? FormaPago { get; set; }
            public decimal Monto { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "Cierres", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Cierres/Index");
            }

            var cierre = _context.CierresCaja.FirstOrDefault(c => c.Id == id);
            if (cierre == null)
            {
                TempData["Error"] = "Cierre no encontrado";
                return RedirectToPage("/Finanzas/Cierres/Index");
            }

            Cierre = cierre;
            Cuenta = _context.CuentasFinancieras.FirstOrDefault(c => c.Id == cierre.CuentaId) ?? new CuentaFinanciera();
            UsuarioCierra = _context.Usuarios.FirstOrDefault(u => u.Id == cierre.UsuarioCierraId) ?? new Usuario();
            UsuarioAprueba = cierre.UsuarioApruebaId.HasValue
                ? _context.Usuarios.FirstOrDefault(u => u.Id == cierre.UsuarioApruebaId.Value)
                : null;
            Apertura = cierre.AperturaId.HasValue
                ? _context.AperturasCaja.FirstOrDefault(a => a.Id == cierre.AperturaId.Value)
                : null;
            Empresa = _context.ConfiguracionEmpresa.FirstOrDefault() ?? new ConfiguracionEmpresa { Nombre = "Mi Empresa" };

            if (cierre.MovimientoAjusteId.HasValue)
            {
                MovimientoAjuste = _context.MovimientosFinancieros
                    .FirstOrDefault(m => m.Id == cierre.MovimientoAjusteId.Value);
            }

            var inicioDia = cierre.Fecha.Date;
            var finDia = inicioDia.AddDays(1);

            MovimientosDia = _context.MovimientosFinancieros
                .AsNoTracking()
                .Where(m => m.Estado == "Activo"
                         && m.Fecha >= inicioDia
                         && m.Fecha < finDia
                         && (m.CuentaId == cierre.CuentaId || m.CuentaDestinoId == cierre.CuentaId))
                .OrderBy(m => m.Fecha)
                .Select(m => new MovimientoResumen
                {
                    Id = m.Id,
                    Numero = m.Numero,
                    Fecha = m.Fecha,
                    Tipo = m.Tipo,
                    Concepto = m.Concepto,
                    FormaPago = m.FormaPago,
                    Monto = m.Monto
                })
                .ToList();

            var (infDesde, infHasta) = InformeMetodosPagoHelper.RangoDeCierre(_context, cierre);
            Informe = InformeMetodosPagoHelper.Generar(_context, infDesde, infHasta, null);
            Informe.Efectivo = new EfectivoCaja
            {
                SaldoInicial = cierre.SaldoInicialSistema, Ingresos = cierre.TotalIngresosEfectivo,
                Egresos = cierre.TotalEgresosEfectivo, Esperado = cierre.EfectivoEsperado
            };

            Conteos = _context.ConteosCierreCaja.AsNoTracking().Where(c => c.CierreCajaId == cierre.Id).OrderBy(c => c.Id).ToList();

            Distribuciones = DistribucionHelper.ObtenerDistribuciones(_context, cierre.Id);
            PorDepositar = EfectivoPorDepositarHelper.Pendientes(_context, cierre.Id);
            Bancos = _context.CuentasFinancieras.Where(c => c.Activa && c.Tipo == "Banco").OrderBy(c => c.Nombre).ToList();

            Adjuntos = _context.AdjuntosCierre
                .Where(a => a.CierreCajaId == cierre.Id)
                .OrderByDescending(a => a.FechaSubida)
                .ToList();

            PuedeAprobar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CierresAprobar", "editar");
            PuedeSubirAdjunto = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CierresCreate", "crear");

            return Page();
        }

        /// <summary>El efectivo «por depositar» llegó al banco: banco, fecha, boleta y (opcional) la foto.</summary>
        public IActionResult OnPostRegistrarDeposito(int id, int distribucionId, int bancoId, DateTime fecha, string? referencia, IFormFile? archivo)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CierresCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Cierres/Details", new { id });
            }

            MovimientoFinanciero? mov;
            using (var tx = _context.Database.BeginTransaction())
            {
                string? error;
                (mov, error) = EfectivoPorDepositarHelper.RegistrarDeposito(_context, distribucionId, bancoId, fecha, referencia, currentUser!.Id);
                if (error != null)
                {
                    tx.Rollback();
                    TempData["Error"] = error;
                    return RedirectToPage("/Finanzas/Cierres/Details", new { id });
                }
                if (archivo != null && archivo.Length > 0)
                {
                    var (_, errAdj) = AdjuntoCierreHelper.Guardar(_context, _env, id, archivo, $"Boleta de depósito {mov!.Referencia}", currentUser.Id);
                    if (errAdj != null)
                    {
                        tx.Rollback();
                        TempData["Error"] = "No se registró el depósito: " + errAdj;
                        return RedirectToPage("/Finanzas/Cierres/Details", new { id });
                    }
                }
                tx.Commit();
            }
            ActividadHelper.Registrar(_context, currentUser.Id, "Depósito de cierre",
                $"{mov!.Concepto}: L. {mov.Monto:N2}, boleta {mov.Referencia}", HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = $"Depósito registrado: L. {mov.Monto:N2} pasaron al banco (boleta {mov.Referencia}).";
            return RedirectToPage("/Finanzas/Cierres/Details", new { id });
        }

        public IActionResult OnPostSubirAdjunto(int id, IFormFile archivo, string? descripcion)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CierresCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Cierres/Details", new { id });
            }

            if (archivo == null || archivo.Length == 0)
            {
                TempData["Error"] = "Debes seleccionar un archivo";
                return RedirectToPage("/Finanzas/Cierres/Details", new { id });
            }

            var (adjunto, error) = AdjuntoCierreHelper.Guardar(
                _context, _env, id, archivo, descripcion, currentUser?.Id);

            if (error != null)
            {
                TempData["Error"] = error;
            }
            else
            {
                TempData["Success"] = "Comprobante subido correctamente";
            }

            return RedirectToPage("/Finanzas/Cierres/Details", new { id });
        }

        public IActionResult OnPostEliminarAdjunto(int id, int adjuntoId)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CierresCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Cierres/Details", new { id });
            }

            // Validar pertenencia
            var adjunto = _context.AdjuntosCierre.FirstOrDefault(a => a.Id == adjuntoId && a.CierreCajaId == id);
            if (adjunto == null)
            {
                TempData["Error"] = "Adjunto no encontrado";
                return RedirectToPage("/Finanzas/Cierres/Details", new { id });
            }

            AdjuntoCierreHelper.Eliminar(_context, _env, adjuntoId);
            TempData["Success"] = "Adjunto eliminado";
            return RedirectToPage("/Finanzas/Cierres/Details", new { id });
        }
    }
}