using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Finanzas.Cierres
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CreateModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<CuentaFinanciera> Cuentas { get; set; } = new();
        public List<CuentaFinanciera> CuentasBancarias { get; set; } = new();
        public List<Usuario> Usuarios { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Debes seleccionar una cuenta")]
            public int CuentaId { get; set; }

            [Required]
            public DateTime Fecha { get; set; } = DateTime.Today;

            [Required(ErrorMessage = "Debes ingresar el efectivo contado")]
            [Range(0, double.MaxValue, ErrorMessage = "El monto no puede ser negativo")]
            public decimal EfectivoContado { get; set; }

            [StringLength(500)]
            public string? Notas { get; set; }

            public decimal Tolerancia { get; set; } = 20.00m;

            public decimal? MontoRetiroBanco { get; set; }
            public int? CuentaBancoDestinoId { get; set; }
            public string? ReferenciaRetiro { get; set; }

            public decimal? MontoFondoCaja { get; set; }

            public decimal? MontoEntregaAdmin { get; set; }
            public int? UsuarioRecibeEntregaId { get; set; }
            public string? NombreRecibeEntrega { get; set; }
            public string? ReferenciaEntrega { get; set; }

            public List<IFormFile>? Adjuntos { get; set; }

            /// <summary>Cerrar sin contar el efectivo ni los comprobantes: exige contraseña de un administrador.</summary>
            public bool SinConteo { get; set; }
            public string? AutorizadorUsuario { get; set; }
            public string? AutorizadorPassword { get; set; }

            /// <summary>Conteo de vouchers/comprobantes por método (JSON generado en pantalla).</summary>
            public string? ConteoJson { get; set; }
        }

        /// <summary>Informe previo del día (antes de cerrar): se carga en la pantalla al elegir la cuenta.</summary>
        public IActionResult OnGetPrevio(int cuentaId, DateTime? fecha)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CierresCreate", "crear"))
                return new ForbidResult();

            var inf = InformeMetodosPagoHelper.GenerarPrevio(_context, cuentaId, (fecha ?? DateTime.Today).Date);
            return Partial("_InformePrevio", inf);
        }

        /// <summary>Guarda el conteo por método (efectivo + lo que se contó en pantalla) en CierreCajaConteo.</summary>
        private void GuardarConteos(CierreCaja cierre)
        {
            var filas = new List<CierreCajaConteo>();
            if (!Input.SinConteo)
                filas.Add(new CierreCajaConteo { CierreCajaId = cierre.Id, Metodo = "Efectivo", EsperadoSistema = cierre.EfectivoEsperado, Contado = cierre.EfectivoContado, Diferencia = cierre.EfectivoContado - cierre.EfectivoEsperado });

            if (!string.IsNullOrWhiteSpace(Input.ConteoJson) && Input.ConteoJson.Length <= 4000)
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(Input.ConteoJson);
                    if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var e in doc.RootElement.EnumerateArray().Take(12))
                        {
                            var n = e.TryGetProperty("n", out var pn) ? pn.GetString() : null;
                            if (string.IsNullOrWhiteSpace(n)) continue;
                            n = n.Trim(); if (n.Length > 60) n = n[..60];
                            decimal s = e.TryGetProperty("s", out var ps) && ps.ValueKind == System.Text.Json.JsonValueKind.Number && ps.TryGetDecimal(out var sv) ? sv : 0;
                            if (!e.TryGetProperty("c", out var pc) || pc.ValueKind != System.Text.Json.JsonValueKind.Number || !pc.TryGetDecimal(out var c)) continue;
                            if (c < 0 || s < 0) continue;
                            filas.Add(new CierreCajaConteo { CierreCajaId = cierre.Id, Metodo = n, EsperadoSistema = s, Contado = c, Diferencia = c - s });
                        }
                    }
                }
                catch (System.Text.Json.JsonException) { }
            }

            if (filas.Count == 0) return;
            _context.ConteosCierreCaja.AddRange(filas);
            _context.SaveChanges();
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CierresCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Cierres/Index");
            }

            CargarDatos();
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CierresCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Cierres/Index");
            }

            CargarDatos();

            // Sin conteo físico no se exige el monto contado (se toma el esperado por el sistema)
            if (Input.SinConteo) ModelState.Remove("Input.EfectivoContado");

            if (!ModelState.IsValid) return Page();

            // Autorización de un administrador para cerrar sin contar
            Usuario? autorizo = null;
            if (Input.SinConteo)
            {
                if (!AutorizacionHelper.VerificarAdmin(_context, Input.AutorizadorUsuario, Input.AutorizadorPassword, out autorizo, out var errAut))
                {
                    ActividadHelper.Registrar(_context, currentUser!.Id, "Cierre sin conteo: autorización rechazada",
                        $"Intento de cerrar sin conteo con autorizador “{Input.AutorizadorUsuario}”", HttpContext.Connection.RemoteIpAddress?.ToString());
                    ModelState.AddModelError(string.Empty, errAut);
                    return Page();
                }
                var calc = CierreHelper.Calcular(_context, Input.CuentaId, Input.Fecha);
                if (calc.Error != null)
                {
                    ModelState.AddModelError(string.Empty, calc.Error);
                    return Page();
                }
                Input.EfectivoContado = calc.EfectivoEsperado;
                Input.MontoRetiroBanco = null; Input.MontoFondoCaja = null; Input.MontoEntregaAdmin = null;
            }

            // Notas: marca de cierre sin conteo + conteo por método (límite de la columna: 500)
            var extras = new List<string>();
            if (Input.SinConteo) extras.Add($"[CIERRE SIN CONTEO FÍSICO, autorizado por {autorizo!.Username}]");
            if (extras.Count > 0)
            {
                var notasFinal = string.Join("\n", extras) + (string.IsNullOrWhiteSpace(Input.Notas) ? "" : "\n" + Input.Notas.Trim());
                Input.Notas = notasFinal.Length > 380 ? notasFinal[..380] : notasFinal;
            }

            if (Input.EfectivoContado < 0 ||
                (Input.MontoRetiroBanco ?? 0) < 0 || (Input.MontoFondoCaja ?? 0) < 0 || (Input.MontoEntregaAdmin ?? 0) < 0)
            {
                ModelState.AddModelError(string.Empty, "Los montos del cierre y de las distribuciones no pueden ser negativos");
                return Page();
            }

            if (Input.Fecha.Date > DateTime.Today)
            {
                ModelState.AddModelError(string.Empty, "No se puede cerrar caja con fecha futura");
                return Page();
            }

            // Validar distribuciones
            var totalDistribuido = (Input.MontoRetiroBanco ?? 0) + (Input.MontoFondoCaja ?? 0) + (Input.MontoEntregaAdmin ?? 0);

            if (!Input.SinConteo && Math.Abs(totalDistribuido - Input.EfectivoContado) > 0.01m)
            {
                ModelState.AddModelError(string.Empty,
                    $"La suma de las distribuciones (L. {totalDistribuido:N2}) no coincide con el efectivo contado (L. {Input.EfectivoContado:N2})");
                return Page();
            }

            // ===== VALIDACIÓN: período contable cerrado =====
            var (periodoOk, periodoError) = CierreContableHelper.ValidarFecha(_context, Input.Fecha);
            if (!periodoOk)
            {
                ModelState.AddModelError(string.Empty, periodoError!);
                return Page();
            }

            // Armado de las distribuciones (antes de guardar nada)
            var distribuciones = new List<DistribucionInput>();

            if (Input.MontoRetiroBanco.HasValue && Input.MontoRetiroBanco > 0 && Input.CuentaBancoDestinoId.HasValue)
            {
                distribuciones.Add(new DistribucionInput
                {
                    Tipo = "RetiroBanco",
                    Monto = Input.MontoRetiroBanco.Value,
                    CuentaDestinoId = Input.CuentaBancoDestinoId,
                    Referencia = Input.ReferenciaRetiro
                });
            }

            if (Input.MontoFondoCaja.HasValue && Input.MontoFondoCaja > 0)
            {
                distribuciones.Add(new DistribucionInput
                {
                    Tipo = "FondoCaja",
                    Monto = Input.MontoFondoCaja.Value,
                    DestinoDescripcion = "Fondo para el próximo día"
                });
            }

            if (Input.MontoEntregaAdmin.HasValue && Input.MontoEntregaAdmin > 0)
            {
                distribuciones.Add(new DistribucionInput
                {
                    Tipo = "EntregaAdmin",
                    Monto = Input.MontoEntregaAdmin.Value,
                    UsuarioRecibeId = Input.UsuarioRecibeEntregaId,
                    NombreRecibe = Input.NombreRecibeEntrega,
                    Referencia = Input.ReferenciaEntrega
                });
            }

            // Cierre + ajuste por sobrante/faltante + distribuciones del efectivo: todo en UNA transacción.
            // Antes, si las distribuciones fallaban el cierre ya quedaba guardado (caja cerrada y el efectivo
            // sin destino registrado). Ahora, si algo falla no se guarda nada y se puede corregir y reintentar.
            CierreCaja cierre;
            using (var txCierre = _context.Database.BeginTransaction())
            {
                var (cierreReg, error) = CierreHelper.Registrar(
                    _context,
                    Input.CuentaId,
                    Input.Fecha,
                    Input.EfectivoContado,
                    Input.Notas,
                    Input.Tolerancia,
                    currentUser!.Id
                );

                if (error != null)
                {
                    txCierre.Rollback();
                    _context.ChangeTracker.Clear();
                    ModelState.AddModelError(string.Empty, error);
                    return Page();
                }

                if (!Input.SinConteo)
                {
                    var (okDist, distError) = DistribucionHelper.RegistrarDistribuciones(
                        _context, cierreReg.Id, Input.CuentaId, Input.EfectivoContado,
                        distribuciones, currentUser.Id);

                    if (!okDist)
                    {
                        txCierre.Rollback();
                        _context.ChangeTracker.Clear();
                        ModelState.AddModelError(string.Empty,
                            $"No se cerró la caja: hubo un problema con las distribuciones. {distError}");
                        return Page();
                    }
                }

                txCierre.Commit();
                cierre = cierreReg;
            }

            // Conteo por método de pago (tabla propia)
            try { GuardarConteos(cierre); }
            catch (Exception ex) { TempData["Warning"] = "El cierre se registró pero no se pudo guardar el conteo por método: " + ex.Message; }

            // Subir adjuntos
            if (Input.Adjuntos != null && Input.Adjuntos.Count > 0)
            {
                foreach (var archivo in Input.Adjuntos)
                {
                    AdjuntoCierreHelper.Guardar(_context, _env, cierre.Id, archivo, null, currentUser.Id);
                }
            }

            ActividadHelper.Registrar(
                _context,
                currentUser.Id,
                "Cierre de caja",
                $"Cerró caja {cierre.Numero}. Esperado: L. {cierre.EfectivoEsperado:N2}, Contado: L. {cierre.EfectivoContado:N2}, Diferencia: L. {cierre.Diferencia:N2}" +
                (Input.SinConteo ? $". SIN CONTEO FÍSICO, autorizó: {autorizo!.Username}" : ""),
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Cierre {cierre.Numero} registrado. Resultado: {cierre.Resultado}";
            return RedirectToPage("/Finanzas/Cierres/Details", new { id = cierre.Id });
        }

        private void CargarDatos()
        {
            Cuentas = _context.CuentasFinancieras
                .Where(c => c.Tipo == "Caja" && c.Activa)
                .OrderBy(c => c.Nombre)
                .ToList();

            CuentasBancarias = _context.CuentasFinancieras
                .Where(c => c.Tipo == "Banco" && c.Activa)
                .OrderBy(c => c.Nombre)
                .ToList();

            Usuarios = _context.Usuarios
                .Where(u => u.Activo)
                .OrderBy(u => u.Username)
                .ToList();
        }
    }
}