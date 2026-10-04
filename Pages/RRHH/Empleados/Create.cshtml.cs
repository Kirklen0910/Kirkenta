using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.RRHH;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.RRHH.Empleados
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

        [BindProperty]
        public IFormFile? Foto { get; set; }

        public string CodigoPreview { get; set; } = "";

        public class InputModel
        {
            // ===== DATOS PERSONALES =====
            [Required(ErrorMessage = "Los nombres son obligatorios")]
            [StringLength(150)]
            public string Nombres { get; set; } = string.Empty;

            [Required(ErrorMessage = "Los apellidos son obligatorios")]
            [StringLength(150)]
            public string Apellidos { get; set; } = string.Empty;

            [Required(ErrorMessage = "La cédula es obligatoria")]
            [StringLength(20)]
            public string Cedula { get; set; } = string.Empty;

            [StringLength(20)]
            public string? RTN { get; set; }

            public DateTime? FechaNacimiento { get; set; }

            [StringLength(30)]
            public string? EstadoCivil { get; set; }

            [StringLength(30)]
            public string? Genero { get; set; }

            [StringLength(50)]
            public string? Nacionalidad { get; set; } = "Hondureña";

            // ===== CONTACTO =====
            [StringLength(30)]
            public string? Telefono { get; set; }

            [StringLength(30)]
            public string? TelefonoSecundario { get; set; }

            [EmailAddress(ErrorMessage = "Email inválido")]
            [StringLength(150)]
            public string? Email { get; set; }

            [StringLength(300)]
            public string? Direccion { get; set; }

            [StringLength(100)]
            public string? Ciudad { get; set; }

            [StringLength(80)]
            public string? Pais { get; set; } = "Honduras";

            // ===== CONTACTO DE EMERGENCIA =====
            [StringLength(150)]
            public string? ContactoEmergenciaNombre { get; set; }

            [StringLength(30)]
            public string? ContactoEmergenciaTelefono { get; set; }

            [StringLength(50)]
            public string? ContactoEmergenciaParentesco { get; set; }

            // ===== DATOS LABORALES =====
            [StringLength(100)]
            public string? PuestoNombre { get; set; }

            [StringLength(100)]
            public string? Departamento { get; set; }

            [Required(ErrorMessage = "La fecha de ingreso es obligatoria")]
            public DateTime FechaIngreso { get; set; } = DateTime.Today;

            [Required]
            public string TipoContrato { get; set; } = "Indefinido";

            public DateTime? FechaFinContrato { get; set; }

            [Required]
            public string TipoJornada { get; set; } = "TiempoCompleto";

            [StringLength(150)]
            public string? JefeInmediato { get; set; }

            // ===== SALARIO =====
            [Required(ErrorMessage = "El salario base es obligatorio")]
            [Range(0, double.MaxValue, ErrorMessage = "El salario no puede ser negativo")]
            public decimal SalarioBase { get; set; } = 0;

            [Required]
            public string FrecuenciaPago { get; set; } = "Mensual";

            public decimal? BonoTransporte { get; set; }
            public decimal? BonoAlimentacion { get; set; }
            public decimal? OtrosBonos { get; set; }

            // ===== DEDUCCIONES =====
            public bool CotizaIHSS { get; set; } = true;
            public bool CotizaRAP { get; set; } = true;
            public bool AplicaISR { get; set; } = true;

            // ===== BANCO =====
            [StringLength(100)]
            public string? Banco { get; set; }

            [StringLength(50)]
            public string? CuentaBancaria { get; set; }

            // ===== ESTADO =====
            [Required]
            public string Estado { get; set; } = "Activo";

            public string? Notas { get; set; }

            // ===== USUARIO DEL SISTEMA =====
            public int? UsuarioId { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "EmpleadosCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso para crear empleados";
                return RedirectToPage("/RRHH/Empleados/Index");
            }

            // Sugerir frecuencia de pago de la empresa
            var config = _context.ConfiguracionEmpresa.FirstOrDefault();
            if (config != null)
            {
                Input.FrecuenciaPago = config.RHFrecuenciaPagoDefault;
                Input.CotizaIHSS = config.RHAplicaIHSS;
                Input.CotizaRAP = config.RHAplicaRAP;
                Input.AplicaISR = config.RHAplicaISR;
            }

            CodigoPreview = EmpleadoHelper.PreviewCodigo(_context);
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "EmpleadosCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Empleados/Index");
            }

            CodigoPreview = EmpleadoHelper.PreviewCodigo(_context);

            // Validar cédula única
            if (_context.Empleados.Any(e => e.Cedula == Input.Cedula))
            {
                ModelState.AddModelError("Input.Cedula", "Ya existe un empleado con esta cédula");
            }

            // Validar RTN si viene
            if (!string.IsNullOrEmpty(Input.RTN) &&
                _context.Empleados.Any(e => e.RTN == Input.RTN))
            {
                ModelState.AddModelError("Input.RTN", "Ya existe un empleado con este RTN");
            }

            if (!ModelState.IsValid) return Page();

            var codigo = EmpleadoHelper.GenerarCodigo(_context);

            var empleado = new Empleado
            {
                Codigo = codigo,
                UsuarioId = Input.UsuarioId,
                Nombres = Input.Nombres,
                Apellidos = Input.Apellidos,
                Cedula = Input.Cedula,
                RTN = Input.RTN,
                FechaNacimiento = Input.FechaNacimiento,
                EstadoCivil = Input.EstadoCivil,
                Genero = Input.Genero,
                Nacionalidad = Input.Nacionalidad,
                Telefono = Input.Telefono,
                TelefonoSecundario = Input.TelefonoSecundario,
                Email = Input.Email,
                Direccion = Input.Direccion,
                Ciudad = Input.Ciudad,
                Pais = Input.Pais,
                ContactoEmergenciaNombre = Input.ContactoEmergenciaNombre,
                ContactoEmergenciaTelefono = Input.ContactoEmergenciaTelefono,
                ContactoEmergenciaParentesco = Input.ContactoEmergenciaParentesco,
                PuestoNombre = Input.PuestoNombre,
                Departamento = Input.Departamento,
                FechaIngreso = Input.FechaIngreso,
                TipoContrato = Input.TipoContrato,
                FechaFinContrato = Input.TipoContrato == "Temporal" || Input.TipoContrato == "Obra" || Input.TipoContrato == "Temporada"
                    ? Input.FechaFinContrato
                    : null,
                TipoJornada = Input.TipoJornada,
                JefeInmediato = Input.JefeInmediato,
                SalarioBase = Input.SalarioBase,
                FrecuenciaPago = Input.FrecuenciaPago,
                BonoTransporte = Input.BonoTransporte,
                BonoAlimentacion = Input.BonoAlimentacion,
                OtrosBonos = Input.OtrosBonos,
                CotizaIHSS = Input.CotizaIHSS,
                CotizaRAP = Input.CotizaRAP,
                AplicaISR = Input.AplicaISR,
                Banco = Input.Banco,
                CuentaBancaria = Input.CuentaBancaria,
                Estado = Input.Estado,
                Notas = Input.Notas,
                FechaCreacion = DateTime.Now,
                UsuarioCreoId = currentUser?.Id,
                EmpresaId = 1
            };

            // Procesar acumulación de vacaciones retroactiva (si ya tenía antigüedad)
            var aniosAntiguedad = EmpleadoHelper.CalcularAniosAntiguedad(empleado, DateTime.Today);
            if (aniosAntiguedad >= 1)
            {
                // Procesar la acumulación de una vez
                decimal totalDias = 0;
                for (int anio = 1; anio <= aniosAntiguedad; anio++)
                {
                    totalDias += EmpleadoHelper.CalcularDiasVacacionesPorAntiguedad(_context, empleado, anio);
                }
                empleado.DiasVacacionesGanados = totalDias;
                empleado.DiasVacacionesDisponibles = totalDias;
                empleado.UltimoAnioVacacionesProcesado = aniosAntiguedad;
            }

            _context.Empleados.Add(empleado);
            _context.SaveChanges();

            // Subir foto si se envió
            if (Foto != null && Foto.Length > 0)
            {
                var (ok, error) = AdjuntoEmpleadoHelper.GuardarFotoPerfil(_context, _env, empleado.Id, Foto);
                if (!ok)
                {
                    TempData["Warning"] = $"Empleado creado pero hubo un problema con la foto: {error}";
                }
            }

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear empleado",
                $"Registró al empleado {empleado.Codigo} — {empleado.Nombres} {empleado.Apellidos}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Empleado {empleado.Codigo} creado correctamente";
            return RedirectToPage("/RRHH/Empleados/Details", new { id = empleado.Id });
        }
    }
}