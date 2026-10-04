using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Empleado de la empresa. Contiene todos los datos personales, laborales
    /// y de contacto. Se liga opcionalmente a un Usuario del sistema.
    /// </summary>
    public class Empleado
    {
        public int Id { get; set; }

        // ===== CÓDIGO Y USUARIO =====
        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty;

        /// <summary>
        /// Usuario del sistema vinculado (opcional). Si tiene, puede
        /// solicitar vacaciones online.
        /// </summary>
        public int? UsuarioId { get; set; }

        // ===== FOTO =====
        [StringLength(300)]
        public string? FotoPath { get; set; }

        // ===== DATOS PERSONALES =====
        [Required]
        [StringLength(150)]
        public string Nombres { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Apellidos { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Cedula { get; set; } = string.Empty;

        [StringLength(20)]
        public string? RTN { get; set; }

        public DateTime? FechaNacimiento { get; set; }

        [StringLength(30)]
        public string? EstadoCivil { get; set; } // Soltero, Casado, Divorciado, Viudo, Unión libre

        [StringLength(30)]
        public string? Genero { get; set; } // Masculino, Femenino, Otro

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
        public string? Pais { get; set; } = "Honduras";

        // ===== CONTACTO DE EMERGENCIA =====
        [StringLength(150)]
        public string? ContactoEmergenciaNombre { get; set; }

        [StringLength(30)]
        public string? ContactoEmergenciaTelefono { get; set; }

        [StringLength(50)]
        public string? ContactoEmergenciaParentesco { get; set; }

        // ===== DATOS LABORALES =====
        public int? PuestoId { get; set; }

        [StringLength(100)]
        public string? PuestoNombre { get; set; }

        [StringLength(100)]
        public string? Departamento { get; set; }

        public DateTime FechaIngreso { get; set; } = DateTime.Today;

        public DateTime? FechaBaja { get; set; }

        [StringLength(30)]
        public string TipoContrato { get; set; } = "Indefinido"; // Indefinido, Temporal, Obra, Temporada

        public DateTime? FechaFinContrato { get; set; }

        [StringLength(30)]
        public string TipoJornada { get; set; } = "TiempoCompleto"; // TiempoCompleto, MedioTiempo, PorHora

        [StringLength(150)]
        public string? JefeInmediato { get; set; }

        // ===== SALARIO =====
        public decimal SalarioBase { get; set; } = 0;

        [StringLength(20)]
        public string FrecuenciaPago { get; set; } = "Mensual"; // Semanal, Catorcenal, Quincenal, Mensual

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
        /// <summary>
        /// Activo | Inactivo | Suspendido | Vacaciones | Licencia | Baja
        /// </summary>
        [StringLength(30)]
        public string Estado { get; set; } = "Activo";

        // ===== VACACIONES (SALDOS) =====
        /// <summary>
        /// Días acumulados disponibles para tomar
        /// </summary>
        public decimal DiasVacacionesDisponibles { get; set; } = 0;

        /// <summary>
        /// Total de días que ha ganado por antigüedad (histórico)
        /// </summary>
        public decimal DiasVacacionesGanados { get; set; } = 0;

        /// <summary>
        /// Total de días que ha tomado (histórico)
        /// </summary>
        public decimal DiasVacacionesTomados { get; set; } = 0;

        /// <summary>
        /// Último año de antigüedad procesado (para acumulación anual)
        /// </summary>
        public int UltimoAnioVacacionesProcesado { get; set; } = 0;

        // ===== NOTAS =====
        public string? Notas { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }
}