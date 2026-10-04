using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.RRHH;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.RRHH.Empleados
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public EditModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty]
        public IFormFile? NuevaFoto { get; set; }

        public string Codigo { get; set; } = "";
        public string? FotoActual { get; set; }

        public class InputModel
        {
            public int Id { get; set; }

            // ===== DATOS PERSONALES =====
            [Required(ErrorMessage = "Los nombres son obligatorios")]
            [StringLength(150)]
            public string Nombres { get; set; } = string.Empty;

            [Required(ErrorMessage = "Los apellidos son obligatorios")]
            [StringLength(150)]
            public string Apellidos { get; set; } = string.Empty;

            [Required]
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
            public string? Nacionalidad { get; set; }

            // ===== CONTACTO =====
            [StringLength(30)]
            public string? Telefono { get; set; }

            [StringLength(30)]
            public string? TelefonoSecundario { get; set; }

            [EmailAddress]
            [StringLength(150)]
            public string? Email { get; set; }

            [StringLength(300)]
            public string? Direccion { get; set; }

            [StringLength(100)]
            public string? Ciudad { get; set; }

            [StringLength(80)]
            public string? Pais { get; set; }

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

            [Required]
            public DateTime FechaIngreso { get; set; }

            public DateTime? FechaBaja { get; set; }

            [Required]
            public string TipoContrato { get; set; } = "Indefinido";

            public DateTime? FechaFinContrato { get; set; }

            [Required]
            public string TipoJornada { get; set; } = "TiempoCompleto";

            [StringLength(150)]
            public string? JefeInmediato { get; set; }

            // ===== SALARIO =====
            [Required]
            [Range(0, double.MaxValue)]
            public decimal SalarioBase { get; set; } = 0;

            [Required]
            public string FrecuenciaPago { get; set; } = "Mensual";

            public decimal? BonoTransporte { get; set; }
            public decimal? BonoAlimentacion { get; set; }
            public decimal? OtrosBonos { get; set; }

            // ===== DEDUCCIONES =====
            public bool CotizaIHSS { get; set; }
            public bool CotizaRAP { get; set; }
            public bool AplicaISR { get; set; }

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

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "EmpleadosEdit", "editar"))
            {
                TempData["Error"] = "No tienes permiso para editar empleados";
                return RedirectToPage("/RRHH/Empleados/Index");
            }

            var empleado = _context.Empleados.FirstOrDefault(e => e.Id == id);
            if (empleado == null)
            {
                TempData["Error"] = "Empleado no encontrado";
                return RedirectToPage("/RRHH/Empleados/Index");
            }

            Codigo = empleado.Codigo;
            FotoActual = empleado.FotoPath;

            Input = new InputModel
            {
                Id = empleado.Id,
                Nombres = empleado.Nombres,
                Apellidos = empleado.Apellidos,
                Cedula = empleado.Cedula,
                RTN = empleado.RTN,
                FechaNacimiento = empleado.FechaNacimiento,
                EstadoCivil = empleado.EstadoCivil,
                Genero = empleado.Genero,
                Nacionalidad = empleado.Nacionalidad,
                Telefono = empleado.Telefono,
                TelefonoSecundario = empleado.TelefonoSecundario,
                Email = empleado.Email,
                Direccion = empleado.Direccion,
                Ciudad = empleado.Ciudad,
                Pais = empleado.Pais,
                ContactoEmergenciaNombre = empleado.ContactoEmergenciaNombre,
                ContactoEmergenciaTelefono = empleado.ContactoEmergenciaTelefono,
                ContactoEmergenciaParentesco = empleado.ContactoEmergenciaParentesco,
                PuestoNombre = empleado.PuestoNombre,
                Departamento = empleado.Departamento,
                FechaIngreso = empleado.FechaIngreso,
                FechaBaja = empleado.FechaBaja,
                TipoContrato = empleado.TipoContrato,
                FechaFinContrato = empleado.FechaFinContrato,
                TipoJornada = empleado.TipoJornada,
                JefeInmediato = empleado.JefeInmediato,
                SalarioBase = empleado.SalarioBase,
                FrecuenciaPago = empleado.FrecuenciaPago,
                BonoTransporte = empleado.BonoTransporte,
                BonoAlimentacion = empleado.BonoAlimentacion,
                OtrosBonos = empleado.OtrosBonos,
                CotizaIHSS = empleado.CotizaIHSS,
                CotizaRAP = empleado.CotizaRAP,
                AplicaISR = empleado.AplicaISR,
                Banco = empleado.Banco,
                CuentaBancaria = empleado.CuentaBancaria,
                Estado = empleado.Estado,
                Notas = empleado.Notas,
                UsuarioId = empleado.UsuarioId
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "EmpleadosEdit", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Empleados/Index");
            }

            var empleado = _context.Empleados.FirstOrDefault(e => e.Id == Input.Id);
            if (empleado == null)
            {
                TempData["Error"] = "Empleado no encontrado";
                return RedirectToPage("/RRHH/Empleados/Index");
            }

            Codigo = empleado.Codigo;
            FotoActual = empleado.FotoPath;

            // Validar cédula única (excluyendo el propio)
            if (_context.Empleados.Any(e => e.Cedula == Input.Cedula && e.Id != Input.Id))
            {
                ModelState.AddModelError("Input.Cedula", "Ya existe otro empleado con esta cédula");
            }

            if (!string.IsNullOrEmpty(Input.RTN) &&
                _context.Empleados.Any(e => e.RTN == Input.RTN && e.Id != Input.Id))
            {
                ModelState.AddModelError("Input.RTN", "Ya existe otro empleado con este RTN");
            }

            if (!ModelState.IsValid) return Page();

            empleado.Nombres = Input.Nombres;
            empleado.Apellidos = Input.Apellidos;
            empleado.Cedula = Input.Cedula;
            empleado.RTN = Input.RTN;
            empleado.FechaNacimiento = Input.FechaNacimiento;
            empleado.EstadoCivil = Input.EstadoCivil;
            empleado.Genero = Input.Genero;
            empleado.Nacionalidad = Input.Nacionalidad;
            empleado.Telefono = Input.Telefono;
            empleado.TelefonoSecundario = Input.TelefonoSecundario;
            empleado.Email = Input.Email;
            empleado.Direccion = Input.Direccion;
            empleado.Ciudad = Input.Ciudad;
            empleado.Pais = Input.Pais;
            empleado.ContactoEmergenciaNombre = Input.ContactoEmergenciaNombre;
            empleado.ContactoEmergenciaTelefono = Input.ContactoEmergenciaTelefono;
            empleado.ContactoEmergenciaParentesco = Input.ContactoEmergenciaParentesco;
            empleado.PuestoNombre = Input.PuestoNombre;
            empleado.Departamento = Input.Departamento;
            empleado.FechaIngreso = Input.FechaIngreso;
            empleado.FechaBaja = Input.Estado == "Baja"
                ? (Input.FechaBaja ?? DateTime.Today)
                : null;
            empleado.TipoContrato = Input.TipoContrato;
            empleado.FechaFinContrato = Input.TipoContrato == "Temporal" || Input.TipoContrato == "Obra" || Input.TipoContrato == "Temporada"
                ? Input.FechaFinContrato
                : null;
            empleado.TipoJornada = Input.TipoJornada;
            empleado.JefeInmediato = Input.JefeInmediato;
            empleado.SalarioBase = Input.SalarioBase;
            empleado.FrecuenciaPago = Input.FrecuenciaPago;
            empleado.BonoTransporte = Input.BonoTransporte;
            empleado.BonoAlimentacion = Input.BonoAlimentacion;
            empleado.OtrosBonos = Input.OtrosBonos;
            empleado.CotizaIHSS = Input.CotizaIHSS;
            empleado.CotizaRAP = Input.CotizaRAP;
            empleado.AplicaISR = Input.AplicaISR;
            empleado.Banco = Input.Banco;
            empleado.CuentaBancaria = Input.CuentaBancaria;
            empleado.Estado = Input.Estado;
            empleado.Notas = Input.Notas;
            empleado.UsuarioId = Input.UsuarioId;

            _context.SaveChanges();

            // Subir nueva foto si se envió
            if (NuevaFoto != null && NuevaFoto.Length > 0)
            {
                var (ok, error) = AdjuntoEmpleadoHelper.GuardarFotoPerfil(_context, _env, empleado.Id, NuevaFoto);
                if (!ok)
                {
                    TempData["Warning"] = $"Cambios guardados pero hubo un problema con la foto: {error}";
                }
            }

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Editar empleado",
                $"Editó al empleado {empleado.Codigo} — {empleado.Nombres} {empleado.Apellidos}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Empleado {empleado.Codigo} actualizado";
            return RedirectToPage("/RRHH/Empleados/Details", new { id = empleado.Id });
        }
    }
}