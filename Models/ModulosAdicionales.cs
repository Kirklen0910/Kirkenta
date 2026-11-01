using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kirkenta.Models
{
    // ============================================================
    // ACTIVOS FIJOS
    // ============================================================

    public class ActivoFijo
    {
        public int Id { get; set; }

        [StringLength(30)]
        public string Codigo { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        /// <summary>Mobiliario | Equipo de cómputo | Vehículos | Maquinaria | Otro</summary>
        [StringLength(40)]
        public string Categoria { get; set; } = "Mobiliario";

        public DateTime FechaAdquisicion { get; set; } = DateTime.Today;
        public decimal CostoAdquisicion { get; set; }
        public decimal ValorResidual { get; set; }
        public int VidaUtilMeses { get; set; } = 60;
        public decimal DepreciacionAcumulada { get; set; }

        [StringLength(100)]
        public string? Ubicacion { get; set; }
        [StringLength(100)]
        public string? Responsable { get; set; }
        [StringLength(80)]
        public string? NumeroSerie { get; set; }

        /// <summary>Activo | Baja</summary>
        [StringLength(20)]
        public string Estado { get; set; } = "Activo";

        public DateTime? FechaBaja { get; set; }
        [StringLength(300)]
        public string? MotivoBaja { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [NotMapped]
        public decimal ValorEnLibros => Math.Round(CostoAdquisicion - DepreciacionAcumulada, 2);

        [NotMapped]
        public decimal DepreciacionMensual =>
            VidaUtilMeses <= 0 ? 0 : Math.Round(Math.Max(CostoAdquisicion - ValorResidual, 0) / VidaUtilMeses, 2);
    }

    public class DepreciacionActivo
    {
        public int Id { get; set; }
        public int ActivoFijoId { get; set; }
        public int Anio { get; set; }
        public int Mes { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public int? UsuarioId { get; set; }
    }

    public class MantenimientoActivo
    {
        public int Id { get; set; }
        public int ActivoFijoId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Today;

        /// <summary>Preventivo | Correctivo</summary>
        [StringLength(20)]
        public string Tipo { get; set; } = "Preventivo";

        [Required, StringLength(300)]
        public string Descripcion { get; set; } = string.Empty;

        public decimal Costo { get; set; }
        [StringLength(100)]
        public string? Proveedor { get; set; }
        public DateTime? ProximaFecha { get; set; }

        /// <summary>Programado | Realizado | Cancelado</summary>
        [StringLength(20)]
        public string Estado { get; set; } = "Programado";

        public int? MovimientoFinancieroId { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioId { get; set; }
    }

    // ============================================================
    // PRODUCCIÓN
    // ============================================================

    public class OrdenProduccion
    {
        public int Id { get; set; }
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Today;

        /// <summary>Producto terminado que se fabrica.</summary>
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal CantidadProducida { get; set; }
        public decimal CostoTotal { get; set; }

        /// <summary>Planificada | EnProceso | Terminada | Cancelada</summary>
        [StringLength(20)]
        public string Estado { get; set; } = "Planificada";

        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        [StringLength(500)]
        public string? Notas { get; set; }
        public int? UsuarioId { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }

    public class DetalleOrdenProduccion
    {
        public int Id { get; set; }
        public int OrdenProduccionId { get; set; }
        /// <summary>Materia prima o insumo que se consume.</summary>
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
    }

    public class InspeccionCalidad
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public int? OrdenProduccionId { get; set; }
        public int ProductoId { get; set; }
        public decimal CantidadInspeccionada { get; set; }
        public decimal CantidadDefectuosa { get; set; }

        /// <summary>Aprobado | Condicional | Rechazado</summary>
        [StringLength(20)]
        public string Resultado { get; set; } = "Aprobado";

        [StringLength(500)]
        public string? Observaciones { get; set; }
        [StringLength(100)]
        public string? Inspector { get; set; }
        public int? UsuarioId { get; set; }
    }

    // ============================================================
    // CAJA CHICA
    // ============================================================

    public class GastoCajaChica
    {
        public int Id { get; set; }
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Today;
        public int CuentaId { get; set; }
        public int CategoriaId { get; set; }
        [Required, StringLength(300)]
        public string Concepto { get; set; } = string.Empty;
        [StringLength(100)]
        public string? Beneficiario { get; set; }
        [StringLength(80)]
        public string? Comprobante { get; set; }
        public decimal Monto { get; set; }

        /// <summary>Pendiente | Aprobado | Rechazado</summary>
        [StringLength(20)]
        public string Estado { get; set; } = "Pendiente";

        [StringLength(300)]
        public string? MotivoRechazo { get; set; }
        public int? MovimientoFinancieroId { get; set; }
        public int? UsuarioSolicitaId { get; set; }
        public int? UsuarioApruebaId { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }

    // ============================================================
    // INVENTARIO AVANZADO: TOMA FÍSICA
    // ============================================================

    public class TomaFisica
    {
        public int Id { get; set; }
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Now;
        [StringLength(200)]
        public string? Descripcion { get; set; }

        /// <summary>EnConteo | Aplicada | Cancelada</summary>
        [StringLength(20)]
        public string Estado { get; set; } = "EnConteo";

        public DateTime? FechaAplicacion { get; set; }
        public int? UsuarioId { get; set; }
    }

    public class DetalleTomaFisica
    {
        public int Id { get; set; }
        public int TomaFisicaId { get; set; }
        public int ProductoId { get; set; }
        /// <summary>Stock del sistema al momento de iniciar la toma.</summary>
        public decimal StockSistema { get; set; }
        /// <summary>Cantidad contada (null = aún no contado).</summary>
        public decimal? StockContado { get; set; }
        public decimal CostoUnitario { get; set; }
    }

    // ============================================================
    // CONTABILIDAD: ASIENTOS MANUALES (PARTIDA DOBLE)
    // ============================================================

    public class AsientoContable
    {
        public int Id { get; set; }
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Today;
        [Required, StringLength(300)]
        public string Concepto { get; set; } = string.Empty;
        [StringLength(100)]
        public string? Referencia { get; set; }

        /// <summary>Activo | Anulado</summary>
        [StringLength(20)]
        public string Estado { get; set; } = "Activo";

        [StringLength(300)]
        public string? MotivoAnulacion { get; set; }
        public int? UsuarioId { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }

    public class DetalleAsiento
    {
        public int Id { get; set; }
        public int AsientoContableId { get; set; }
        public int PlanCuentaId { get; set; }
        public decimal Debe { get; set; }
        public decimal Haber { get; set; }
        [StringLength(200)]
        public string? Detalle { get; set; }
    }
}
