using System.ComponentModel.DataAnnotations;
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Configuracion
{
    /// <summary>Asistente de configuración inicial: lleva paso a paso a un negocio nuevo hasta poder vender. Solo Admin.</summary>
    public class InicioModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public InicioModel(ApplicationDbContext db) { _db = db; }

        public class EmpresaForm
        {
            [Required(ErrorMessage = "El nombre del negocio es obligatorio"), StringLength(150)]
            public string Nombre { get; set; } = "";
            [StringLength(200)] public string? RazonSocial { get; set; }
            [StringLength(20), Rtn] public string? RTN { get => _rtn; set => _rtn = DocumentoHonduras.FormatearRtn(value); }
            private string? _rtn;
            [StringLength(300)] public string? Direccion { get; set; }
            [StringLength(100)] public string? Ciudad { get; set; }
            [StringLength(30), Telefono] public string? Telefono { get; set; }
            [EmailAddress, StringLength(150)] public string? Email { get; set; }
        }

        public class FiscalForm
        {
            public bool Activar { get; set; }
            [StringLength(50)] public string? CAI { get; set; }
            [StringLength(30)] public string? RangoInicial { get; set; }
            [StringLength(30)] public string? RangoFinal { get; set; }
            public DateTime? FechaLimite { get; set; }
        }

        public class CajaForm
        {
            [StringLength(100)] public string? NombreCaja { get; set; }
            [Range(0, 100000000)] public decimal FondoInicial { get; set; }
            [StringLength(100)] public string? Banco { get; set; }
            [StringLength(100)] public string? NombreBanco { get; set; }
            [StringLength(50)] public string? NumeroCuenta { get; set; }
            [Range(0, 100000000)] public decimal SaldoBanco { get; set; }
        }

        public class CatalogoForm
        {
            public string? Categorias { get; set; }
            public List<string>? Unidades { get; set; }
            public string? OtrasUnidades { get; set; }
        }

        public int Paso { get; set; } = 1;
        public const int TotalPasos = 5;
        public EmpresaForm Empresa { get; set; } = new();
        public FiscalForm Fiscal { get; set; } = new();
        public CajaForm Caja { get; set; } = new();
        public CatalogoForm Catalogo { get; set; } = new();
        public List<PasoConfiguracion> Lista { get; set; } = new();
        public List<string> Errores { get; set; } = new();
        public bool YaHayCaja { get; set; }
        public int CajasExistentes { get; set; }
        public List<string> CategoriasExistentes { get; set; } = new();
        public HashSet<string> UnidadesExistentes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        private Usuario? Admin()
        {
            var nombre = User.Identity?.Name?.Trim();
            if (string.IsNullOrEmpty(nombre)) return null;
            var u = _db.Usuarios.AsNoTracking().FirstOrDefault(x => x.Username == nombre);
            return u != null && PermisoHelper.UsuarioTieneAcceso(_db, u.Username, "Configuracion", "Inicio") ? u : null;
        }

        private IActionResult SinPermiso()
        {
            TempData["Error"] = "Solo un administrador puede usar el asistente de configuración inicial.";
            return Redirect("/");
        }

        private string Ip() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

        private void Cargar(int paso, bool precargar = true)
        {
            Paso = Math.Clamp(paso, 1, TotalPasos);
            var e = _db.ConfiguracionEmpresa.AsNoTracking().FirstOrDefault();
            if (e != null && precargar)
            {
                Empresa = new EmpresaForm
                {
                    Nombre = e.Nombre == ConfiguracionInicialHelper.NombrePorDefecto ? "" : e.Nombre,
                    RazonSocial = e.RazonSocial, RTN = e.RTN, Direccion = e.Direccion, Ciudad = e.Ciudad, Telefono = e.Telefono, Email = e.Email
                };
                Fiscal = new FiscalForm
                {
                    Activar = e.FacturacionFiscalActiva, CAI = e.CAI, RangoInicial = e.RangoInicial, RangoFinal = e.RangoFinal, FechaLimite = e.FechaLimiteEmision
                };
            }
            CajasExistentes = _db.CuentasFinancieras.Count(c => c.Tipo == "Caja" && c.Activa);
            YaHayCaja = CajasExistentes > 0;
            CategoriasExistentes = _db.Categorias.OrderBy(c => c.Nombre).Select(c => c.Nombre).ToList();
            UnidadesExistentes = new HashSet<string>(_db.UnidadesMedida.Select(u => u.Nombre), StringComparer.OrdinalIgnoreCase);
            Lista = ConfiguracionInicialHelper.Estado(_db);
        }

        public IActionResult OnGet(int? paso)
        {
            if (Admin() == null) return SinPermiso();
            Cargar(paso ?? ProximoPaso());
            return Page();
        }

        /// <summary>Primer paso del asistente que falta; si todo está, la pantalla final.</summary>
        private int ProximoPaso()
        {
            var l = ConfiguracionInicialHelper.Estado(_db);
            if (!l.First(p => p.Clave == "empresa").Completo) return 1;
            if (!l.First(p => p.Clave == "caja").Completo) return 3;
            if (!l.First(p => p.Clave == "catalogo").Completo) return 4;
            return 5;
        }

        // ---------- Paso 1: empresa ----------
        public IActionResult OnPostEmpresa(EmpresaForm form)
        {
            var u = Admin(); if (u == null) return SinPermiso();
            if (!ModelState.IsValid)
            {
                Empresa = form; Cargar(1, false);
                Errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(m => !string.IsNullOrEmpty(m)).Distinct().ToList();
                return Page();
            }

            var e = _db.ConfiguracionEmpresa.FirstOrDefault();
            if (e == null) { e = new ConfiguracionEmpresa(); _db.ConfiguracionEmpresa.Add(e); }
            e.Nombre = form.Nombre.Trim();
            e.RazonSocial = form.RazonSocial?.Trim();
            e.RTN = form.RTN;
            e.Direccion = form.Direccion?.Trim();
            e.Ciudad = form.Ciudad?.Trim();
            e.Telefono = form.Telefono?.Trim();
            e.Email = form.Email?.Trim();
            e.FechaActualizacion = DateTime.Now;
            _db.SaveChanges();

            ActividadHelper.Registrar(_db, u.Id, "Configuración inicial", $"Datos de la empresa: {e.Nombre}", Ip());
            return RedirectToPage(new { paso = 2 });
        }

        // ---------- Paso 2: facturación fiscal ----------
        public IActionResult OnPostFiscal(FiscalForm form, string? omitir)
        {
            var u = Admin(); if (u == null) return SinPermiso();
            if (omitir != null) return RedirectToPage(new { paso = 3 });

            if (form.Activar)
            {
                var error = FacturacionFiscalHelper.ValidarConfiguracion(form.CAI, form.RangoInicial, form.RangoFinal, form.FechaLimite);
                if (error != null)
                {
                    Fiscal = form; Cargar(2, false); Errores.Add(error);
                    return Page();
                }
            }
            if (!ModelState.IsValid)
            {
                Fiscal = form; Cargar(2, false);
                Errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(m => !string.IsNullOrEmpty(m)).ToList();
                return Page();
            }

            var e = _db.ConfiguracionEmpresa.FirstOrDefault();
            if (e == null) { TempData["Error"] = "Primero completa los datos de la empresa."; return RedirectToPage(new { paso = 1 }); }

            bool cambioRango = !string.Equals(e.CAI, form.CAI, StringComparison.Ordinal)
                            || !string.Equals(e.RangoInicial, form.RangoInicial, StringComparison.Ordinal)
                            || !string.Equals(e.RangoFinal, form.RangoFinal, StringComparison.Ordinal);
            e.FacturacionFiscalActiva = form.Activar;
            e.CAI = form.CAI?.Trim();
            e.RangoInicial = form.RangoInicial?.Trim();
            e.RangoFinal = form.RangoFinal?.Trim();
            e.FechaLimiteEmision = form.FechaLimite;
            if (cambioRango) e.SiguienteNumeroFiscal = 0;
            e.FechaActualizacion = DateTime.Now;
            _db.SaveChanges();

            ActividadHelper.Registrar(_db, u.Id, "Configuración inicial", form.Activar ? "Facturación fiscal activada" : "Facturación fiscal sin activar", Ip());
            return RedirectToPage(new { paso = 3 });
        }

        // ---------- Paso 3: caja y banco ----------
        public IActionResult OnPostCaja(CajaForm form, string? omitir)
        {
            var u = Admin(); if (u == null) return SinPermiso();
            if (omitir != null) return RedirectToPage(new { paso = 4 });
            if (!ModelState.IsValid)
            {
                Caja = form; Cargar(3, false);
                Errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(m => !string.IsNullOrEmpty(m)).ToList();
                return Page();
            }

            int monedaId = _db.Monedas.Where(m => m.Activa).OrderBy(m => m.Id).Select(m => m.Id).FirstOrDefault();
            if (monedaId == 0) monedaId = 1;
            var creadas = new List<string>();
            var avisos = new List<string>();

            if (!string.IsNullOrWhiteSpace(form.NombreCaja))
            {
                var caja = new CuentaFinanciera
                {
                    Codigo = ConfiguracionInicialHelper.CodigoCuentaLibre(_db, "CAJA"),
                    Nombre = form.NombreCaja.Trim(), Tipo = "Caja", Subtipo = "POS", MonedaId = monedaId,
                    SaldoInicial = form.FondoInicial, SaldoActual = form.FondoInicial, Activa = true,
                    FechaCreacion = DateTime.Now, UsuarioCreoId = u.Id, EmpresaId = EmpresaActual.Id
                };
                _db.CuentasFinancieras.Add(caja);
                _db.SaveChanges();
                creadas.Add(caja.Nombre);
                if (caja.SaldoInicial > 0)
                {
                    var (_, err) = AperturaCuentaHelper.RegistrarApertura(_db, caja.Id, u.Id);
                    if (err != null) avisos.Add($"Caja creada, pero no se pudo registrar el fondo inicial: {err}");
                }
            }

            if (!string.IsNullOrWhiteSpace(form.Banco))
            {
                var banco = new CuentaFinanciera
                {
                    Codigo = ConfiguracionInicialHelper.CodigoCuentaLibre(_db, "BCO"),
                    Nombre = string.IsNullOrWhiteSpace(form.NombreBanco) ? ("Cuenta " + form.Banco.Trim()) : form.NombreBanco.Trim(),
                    Tipo = "Banco", Banco = form.Banco.Trim(), NumeroCuenta = form.NumeroCuenta?.Trim(), MonedaId = monedaId,
                    SaldoInicial = form.SaldoBanco, SaldoActual = form.SaldoBanco, Activa = true,
                    FechaCreacion = DateTime.Now, UsuarioCreoId = u.Id, EmpresaId = EmpresaActual.Id
                };
                _db.CuentasFinancieras.Add(banco);
                _db.SaveChanges();
                creadas.Add(banco.Nombre);
                if (banco.SaldoInicial > 0)
                {
                    var (_, err) = AperturaCuentaHelper.RegistrarApertura(_db, banco.Id, u.Id);
                    if (err != null) avisos.Add($"Cuenta de banco creada, pero no se pudo registrar el saldo inicial: {err}");
                }
            }

            if (creadas.Count > 0)
            {
                ActividadHelper.Registrar(_db, u.Id, "Configuración inicial", "Cuentas creadas: " + string.Join(", ", creadas), Ip());
                TempData["Success"] = "Cuentas creadas: " + string.Join(", ", creadas) + ".";
            }
            if (avisos.Count > 0) TempData["Warning"] = string.Join(" ", avisos);
            return RedirectToPage(new { paso = 4 });
        }

        // ---------- Paso 4: categorías y unidades ----------
        public IActionResult OnPostCatalogo(CatalogoForm form, string? omitir)
        {
            var u = Admin(); if (u == null) return SinPermiso();
            if (omitir != null) return RedirectToPage(new { paso = 5 });

            int nuevasCat = 0, nuevasUni = 0;

            var existentesCat = new HashSet<string>(_db.Categorias.Select(c => c.Nombre), StringComparer.OrdinalIgnoreCase);
            var colores = ConfiguracionInicialHelper.ColoresCategorias;
            foreach (var nombre in ConfiguracionInicialHelper.ParsearLista(form.Categorias))
            {
                if (existentesCat.Contains(nombre)) continue;
                _db.Categorias.Add(new Categoria { Nombre = nombre, Color = colores[(existentesCat.Count + nuevasCat) % colores.Length], Activa = true });
                existentesCat.Add(nombre); nuevasCat++;
            }

            var existentesUni = new HashSet<string>(_db.UnidadesMedida.Select(x => x.Nombre), StringComparer.OrdinalIgnoreCase);
            var elegidas = new List<(string Nombre, string Abrev)>();
            foreach (var n in form.Unidades ?? new List<string>())
            {
                var sug = ConfiguracionInicialHelper.UnidadesSugeridas.FirstOrDefault(s => s.Nombre == n);
                if (sug.Nombre != null) elegidas.Add(sug);
            }
            foreach (var n in ConfiguracionInicialHelper.ParsearLista(form.OtrasUnidades, 50, 20))
                elegidas.Add((n, n.Length > 10 ? n[..10] : n.ToLowerInvariant()));
            foreach (var (nombre, abrev) in elegidas)
            {
                if (existentesUni.Contains(nombre)) continue;
                _db.UnidadesMedida.Add(new UnidadMedida { Nombre = nombre, Abreviatura = abrev, Activa = true });
                existentesUni.Add(nombre); nuevasUni++;
            }

            if (nuevasCat + nuevasUni > 0)
            {
                _db.SaveChanges();
                ActividadHelper.Registrar(_db, u.Id, "Configuración inicial", $"Catálogo: {nuevasCat} categoría(s) y {nuevasUni} unidad(es) nuevas", Ip());
                TempData["Success"] = $"Se crearon {nuevasCat} categoría(s) y {nuevasUni} unidad(es) de medida.";
            }
            return RedirectToPage(new { paso = 5 });
        }
    }
}
