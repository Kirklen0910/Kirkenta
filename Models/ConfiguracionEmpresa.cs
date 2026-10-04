using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Configuración general de la empresa, incluyendo RRHH, nómina y calendario.
    /// </summary>
    public class ConfiguracionEmpresa
    {
        public int Id { get; set; }

        // ===== DATOS GENERALES (existentes) =====
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(200)]
        public string? RazonSocial { get; set; }

        [StringLength(20)]
        public string? RTN { get; set; }

        [StringLength(300)]
        public string? Direccion { get; set; }

        [StringLength(100)]
        public string? Ciudad { get; set; }

        [StringLength(80)]
        public string? Pais { get; set; } = "Honduras";

        [StringLength(30)]
        public string? Telefono { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(150)]
        public string? SitioWeb { get; set; }

        [StringLength(300)]
        public string? LogoPath { get; set; }

        [StringLength(50)]
        public string? CAI { get; set; }

        [StringLength(30)]
        public string? RangoInicial { get; set; }

        [StringLength(30)]
        public string? RangoFinal { get; set; }

        public DateTime? FechaLimiteEmision { get; set; }

        public string? NotasFactura { get; set; }

        public DateTime FechaActualizacion { get; set; } = DateTime.Now;

        // ===== PAÍS Y ZONA HORARIA (nuevos para feriados) =====
        /// <summary>
        /// Código ISO del país: HN, GT, SV, CR, NI, PA, MX, US
        /// </summary>
        [StringLength(3)]
        public string PaisCodigo { get; set; } = "HN";

        /// <summary>
        /// Zona horaria IANA: America/Tegucigalpa
        /// </summary>
        [StringLength(60)]
        public string ZonaHoraria { get; set; } = "America/Tegucigalpa";

        // ===== CONFIGURACIÓN RRHH =====
        /// <summary>
        /// Frecuencia de pago por defecto: Semanal | Catorcenal | Quincenal | Mensual
        /// </summary>
        [StringLength(20)]
        public string RHFrecuenciaPagoDefault { get; set; } = "Mensual";

        /// <summary>
        /// Día 1 de pago (para quincenal: 15, mensual: 30)
        /// </summary>
        public int RHDiaPago1 { get; set; } = 15;

        /// <summary>
        /// Día 2 de pago (solo quincenal)
        /// </summary>
        public int? RHDiaPago2 { get; set; } = 30;

        /// <summary>
        /// Día de la semana para pago semanal (1 = lunes, 5 = viernes)
        /// </summary>
        public int? RHDiaSemanalPago { get; set; } = 5;

        // ===== DEDUCCIONES (legacy — la config nueva está en ConfiguracionDeduccion) =====
        public bool RHAplicaIHSS { get; set; } = true;
        public decimal RHPorcentajeIHSS { get; set; } = 2.5m;
        public decimal RHTopeIHSS { get; set; } = 11995.00m;

        public bool RHAplicaRAP { get; set; } = true;
        public decimal RHPorcentajeRAP { get; set; } = 1.5m;

        public bool RHAplicaISR { get; set; } = true;

        /// <summary>
        /// Método por defecto para cálculo ISR: Acumulativo | MensualSimple
        /// </summary>
        [StringLength(20)]
        public string RHMetodoISRDefault { get; set; } = "Acumulativo";

        // ===== VACACIONES POR ANTIGÜEDAD =====
        /// <summary>
        /// Tabla de días de vacaciones por año de antigüedad.
        /// Formato: "1:10,2:12,3:15,4:20,5:20"
        /// </summary>
        [StringLength(200)]
        public string RHTablaVacaciones { get; set; } = "1:10,2:12,3:15,4:20,5:20";

        /// <summary>
        /// Años de antigüedad a partir de los cuales se repite el último valor.
        /// En Honduras: a partir del año 5 son 20 días cada año.
        /// </summary>
        public int RHAntiguedadMaxTabla { get; set; } = 5;

        // ===== FERIADOS =====
        /// <summary>
        /// Si el sistema debe cargar automáticamente los feriados del país
        /// </summary>
        public bool RHCargarFeriadosAuto { get; set; } = true;

        // ===== ALERTAS =====
        public bool RHAlertaFeriadosProximos { get; set; } = true;
        public int RHAlertaFeriadosDias { get; set; } = 7;

        public bool RHAlertaVacacionesProximas { get; set; } = true;
        public int RHAlertaVacacionesDias { get; set; } = 7;

        public bool RHAlertaCumpleanios { get; set; } = true;
        public bool RHAlertaAniversarios { get; set; } = true;
        public bool RHAlertaValesPorVencer { get; set; } = true;
        public bool RHAlertaContratosPorVencer { get; set; } = true;
        public int RHAlertaContratosDias { get; set; } = 30;
        public bool RHAlertaDocumentosVencidos { get; set; } = true;
    }
}