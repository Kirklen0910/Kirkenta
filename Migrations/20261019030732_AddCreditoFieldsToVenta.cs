using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kirkenta.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditoFieldsToVenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsCredito",
                table: "Ventas",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaMovimiento",
                table: "Ventas",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVencimientoCredito",
                table: "Ventas",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoEnvio",
                table: "Ventas",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MovimientoCambioId",
                table: "Ventas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MovimientoIngresoId",
                table: "Ventas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MovimientoRegistrado",
                table: "Ventas",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiereEnvio",
                table: "Ventas",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SaldoPendiente",
                table: "Ventas",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "MotivoRechazo",
                table: "ValesEmpleado",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "RowVersion",
                table: "Productos",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "MontoEnvio",
                table: "Pedidos",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "RequiereEnvio",
                table: "Pedidos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Cambio",
                table: "Pagos",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoRecibido",
                table: "Pagos",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoEnvio",
                table: "Facturas",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "RequiereEnvio",
                table: "Facturas",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoEnvio",
                table: "Cotizaciones",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "RequiereEnvio",
                table: "Cotizaciones",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "VentaId",
                table: "Cotizaciones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaisCodigo",
                table: "ConfiguracionEmpresa",
                type: "varchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "RHAlertaAniversarios",
                table: "ConfiguracionEmpresa",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RHAlertaContratosDias",
                table: "ConfiguracionEmpresa",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RHAlertaContratosPorVencer",
                table: "ConfiguracionEmpresa",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RHAlertaCumpleanios",
                table: "ConfiguracionEmpresa",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RHAlertaDocumentosVencidos",
                table: "ConfiguracionEmpresa",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RHAlertaFeriadosDias",
                table: "ConfiguracionEmpresa",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RHAlertaFeriadosProximos",
                table: "ConfiguracionEmpresa",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RHAlertaVacacionesDias",
                table: "ConfiguracionEmpresa",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RHAlertaVacacionesProximas",
                table: "ConfiguracionEmpresa",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RHAlertaValesPorVencer",
                table: "ConfiguracionEmpresa",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RHAntiguedadMaxTabla",
                table: "ConfiguracionEmpresa",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RHAplicaIHSS",
                table: "ConfiguracionEmpresa",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RHAplicaISR",
                table: "ConfiguracionEmpresa",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RHAplicaRAP",
                table: "ConfiguracionEmpresa",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RHCargarFeriadosAuto",
                table: "ConfiguracionEmpresa",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RHDiaPago1",
                table: "ConfiguracionEmpresa",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RHDiaPago2",
                table: "ConfiguracionEmpresa",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RHDiaSemanalPago",
                table: "ConfiguracionEmpresa",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RHFrecuenciaPagoDefault",
                table: "ConfiguracionEmpresa",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RHMetodoISRDefault",
                table: "ConfiguracionEmpresa",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "RHPorcentajeIHSS",
                table: "ConfiguracionEmpresa",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RHPorcentajeRAP",
                table: "ConfiguracionEmpresa",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "RHTablaVacaciones",
                table: "ConfiguracionEmpresa",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "RHTopeIHSS",
                table: "ConfiguracionEmpresa",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ZonaHoraria",
                table: "ConfiguracionEmpresa",
                type: "varchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "PlanCuentaId",
                table: "CategoriasFinancieras",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdjuntosCierre",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CierreCajaId = table.Column<int>(type: "int", nullable: false),
                    NombreArchivo = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RutaArchivo = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TipoArchivo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TamanoKB = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaSubida = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdjuntosCierre", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AdjuntosEmpleado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    TipoDocumentoId = table.Column<int>(type: "int", nullable: false),
                    NombreArchivo = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RutaArchivo = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TipoArchivo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TamanoKB = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaDocumento = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Vigente = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaSubida = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsuarioSubioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdjuntosEmpleado", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AlertasPersonalizadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Titulo = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmpleadoId = table.Column<int>(type: "int", nullable: true),
                    Prioridad = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaAlerta = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MostrarEnDashboard = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaVigenciaHasta = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Completada = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaCompletada = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UsuarioCreoId = table.Column<int>(type: "int", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertasPersonalizadas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AperturasCaja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CuentaId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaApertura = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsuarioAbreId = table.Column<int>(type: "int", nullable: false),
                    SaldoInicial = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Notas = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activa = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CierreId = table.Column<int>(type: "int", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AperturasCaja", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CierresCaja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CuentaId = table.Column<int>(type: "int", nullable: false),
                    AperturaId = table.Column<int>(type: "int", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsuarioCierraId = table.Column<int>(type: "int", nullable: false),
                    SaldoInicialSistema = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TotalIngresosEfectivo = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TotalEgresosEfectivo = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    EfectivoEsperado = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    EfectivoContado = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Diferencia = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Resultado = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tolerancia = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Notas = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalDistribuido = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    EstadoActa = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsuarioApruebaId = table.Column<int>(type: "int", nullable: true),
                    FechaAprobacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MotivoRechazo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MovimientoAjusteId = table.Column<int>(type: "int", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CierresCaja", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CierresContables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Anio = table.Column<int>(type: "int", nullable: false),
                    Mes = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCierre = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UsuarioCierreId = table.Column<int>(type: "int", nullable: true),
                    FechaReapertura = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UsuarioReaperturaId = table.Column<int>(type: "int", nullable: true),
                    MotivoReapertura = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalIngresos = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TotalEgresos = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    CantidadMovimientos = table.Column<int>(type: "int", nullable: false),
                    Notas = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CierresContables", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ConciliacionesBancarias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CuentaId = table.Column<int>(type: "int", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SaldoBanco = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    SaldoSistema = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Diferencia = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Estado = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsuarioCierraId = table.Column<int>(type: "int", nullable: true),
                    FechaCierre = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    NotasCierre = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalLineasSistema = table.Column<int>(type: "int", nullable: false),
                    TotalLineasBanco = table.Column<int>(type: "int", nullable: false),
                    TotalMatcheadas = table.Column<int>(type: "int", nullable: false),
                    TotalNoMatcheadas = table.Column<int>(type: "int", nullable: false),
                    Notas = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsuarioCreoId = table.Column<int>(type: "int", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConciliacionesBancarias", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ConciliacionesDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ConciliacionBancariaId = table.Column<int>(type: "int", nullable: false),
                    Origen = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MovimientoId = table.Column<int>(type: "int", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Descripcion = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Referencia = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Monto = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Matcheada = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MatcheadaConDetalleId = table.Column<int>(type: "int", nullable: true),
                    TipoMatch = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notas = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConciliacionesDetalle", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ConfiguracionDeducciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Anio = table.Column<int>(type: "int", nullable: false),
                    AplicaIHSS = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PorcentajeIHSS = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TopeIHSS = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    AplicaRAP = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PorcentajeRAP = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TopeRAP = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    AplicaISR = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TopeAnualExentoISR = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MetodoISR = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notas = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activa = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsuarioCreoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionDeducciones", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DetalleNominas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NominaId = table.Column<int>(type: "int", nullable: false),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    SalarioBase = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    BonoTransporte = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    BonoAlimentacion = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    OtrosBonos = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    HorasExtra = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MontoHorasExtra = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Comisiones = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TotalBruto = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    DeduccionIHSS = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    DeduccionRAP = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    DeduccionISR = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    DeduccionVales = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    OtrasDeducciones = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TotalDeducciones = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    SalarioNeto = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    DiasTrabajados = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    DiasAusencia = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    DiasVacaciones = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    DiasPermiso = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Notas = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleNominas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DistribucionesCierre",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CierreCajaId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Monto = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    CuentaDestinoId = table.Column<int>(type: "int", nullable: true),
                    DestinoDescripcion = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Referencia = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsuarioRecibeId = table.Column<int>(type: "int", nullable: true),
                    NombreRecibe = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notas = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MovimientoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistribucionesCierre", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Empleados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Codigo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsuarioId = table.Column<int>(type: "int", nullable: true),
                    FotoPath = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombres = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Apellidos = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cedula = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RTN = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaNacimiento = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EstadoCivil = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Genero = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nacionalidad = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telefono = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TelefonoSecundario = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Direccion = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ciudad = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Pais = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContactoEmergenciaNombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContactoEmergenciaTelefono = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContactoEmergenciaParentesco = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PuestoId = table.Column<int>(type: "int", nullable: true),
                    PuestoNombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Departamento = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaIngreso = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaBaja = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    TipoContrato = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaFinContrato = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    TipoJornada = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JefeInmediato = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SalarioBase = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    FrecuenciaPago = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BonoTransporte = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    BonoAlimentacion = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    OtrosBonos = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    CotizaIHSS = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CotizaRAP = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AplicaISR = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Banco = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CuentaBancaria = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Estado = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DiasVacacionesDisponibles = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    DiasVacacionesGanados = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    DiasVacacionesTomados = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    UltimoAnioVacacionesProcesado = table.Column<int>(type: "int", nullable: false),
                    Notas = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsuarioCreoId = table.Column<int>(type: "int", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleados", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Envios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VentaId = table.Column<int>(type: "int", nullable: true),
                    PedidoId = table.Column<int>(type: "int", nullable: true),
                    FacturaId = table.Column<int>(type: "int", nullable: true),
                    CotizacionId = table.Column<int>(type: "int", nullable: true),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    ClienteNombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DireccionEntrega = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Referencia = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContactoNombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContactoTelefono = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ciudad = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ZonaId = table.Column<int>(type: "int", nullable: true),
                    ZonaNombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Monto = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    EsGratis = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Estado = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RutaId = table.Column<int>(type: "int", nullable: true),
                    OrdenParada = table.Column<int>(type: "int", nullable: true),
                    RepartidorId = table.Column<int>(type: "int", nullable: true),
                    RepartidorNombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Vehiculo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaEntregaEstimada = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FechaSalida = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FechaEntregaReal = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    NombreRecibio = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FirmaImagen = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FotoEntrega = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MotivoFallo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notas = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsuarioCreoId = table.Column<int>(type: "int", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Envios", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExpedientesEmpleado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Titulo = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Gravedad = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaInicioSuspension = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FechaFinSuspension = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DiasSuspension = table.Column<int>(type: "int", nullable: true),
                    UsuarioRegistroId = table.Column<int>(type: "int", nullable: true),
                    NombreRegistro = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Testigos = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmpleadoFirmo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaFirma = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RutaDocumentoFirmado = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notas = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpedientesEmpleado", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Feriados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PaisCodigo = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EsRecurrente = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EsMovil = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Anio = table.Column<int>(type: "int", nullable: true),
                    Descripcion = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feriados", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Nominas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PeriodoDescripcion = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaInicio = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Estado = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CantidadEmpleados = table.Column<int>(type: "int", nullable: false),
                    TotalSalariosBrutos = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TotalBonos = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TotalHorasExtra = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TotalDeduccionIHSS = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TotalDeduccionRAP = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TotalDeduccionISR = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TotalDeduccionVales = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TotalOtrasDeducciones = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TotalNeto = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    UsuarioCalculoId = table.Column<int>(type: "int", nullable: true),
                    FechaCalculo = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UsuarioApruebaId = table.Column<int>(type: "int", nullable: true),
                    FechaAprobacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UsuarioPagaId = table.Column<int>(type: "int", nullable: true),
                    FechaPagoReal = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CuentaId = table.Column<int>(type: "int", nullable: true),
                    MovimientoId = table.Column<int>(type: "int", nullable: true),
                    Notas = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nominas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PagosNominaEmpleado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NominaId = table.Column<int>(type: "int", nullable: false),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    DetalleNominaId = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MetodoPago = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Referencia = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CuentaId = table.Column<int>(type: "int", nullable: true),
                    MovimientoId = table.Column<int>(type: "int", nullable: true),
                    FechaPago = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsuarioPagoId = table.Column<int>(type: "int", nullable: true),
                    Estado = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notas = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosNominaEmpleado", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PermisosEmpleado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Estado = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaInicio = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DiasSolicitados = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ConGoceSueldo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsuarioSolicitaId = table.Column<int>(type: "int", nullable: true),
                    Motivo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsuarioApruebaId = table.Column<int>(type: "int", nullable: true),
                    FechaAprobacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MotivoRechazo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RutaDocumentoSoporte = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notas = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermisosEmpleado", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlanCuentas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Codigo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodigoPadre = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nivel = table.Column<int>(type: "int", nullable: false),
                    Naturaleza = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EsMovimiento = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Activa = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Descripcion = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanCuentas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Repartidores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Codigo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telefono = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Vehiculo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Placa = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Licencia = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notas = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsuarioCreoId = table.Column<int>(type: "int", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Repartidores", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Rutas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RepartidorId = table.Column<int>(type: "int", nullable: true),
                    RepartidorNombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Vehiculo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Placa = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Estado = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ZonaId = table.Column<int>(type: "int", nullable: true),
                    ZonaNombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaSalida = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FechaRegreso = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    TotalParadas = table.Column<int>(type: "int", nullable: false),
                    ParadasEntregadas = table.Column<int>(type: "int", nullable: false),
                    ParadasFallidas = table.Column<int>(type: "int", nullable: false),
                    MontoTotalEnvios = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TrackingCode = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notas = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsuarioCreoId = table.Column<int>(type: "int", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rutas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RutasHistorial",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RutaId = table.Column<int>(type: "int", nullable: false),
                    EnvioId = table.Column<int>(type: "int", nullable: true),
                    Evento = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Detalle = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EstadoAnterior = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EstadoNuevo = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: true),
                    UsuarioNombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RutasHistorial", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TiposDocumentoEmpleado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Categoria = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EsObligatorio = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RequiereVencimiento = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DiasAlertaVencimiento = table.Column<int>(type: "int", nullable: true),
                    Icono = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EsSistema = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposDocumentoEmpleado", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TramosISR",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ConfiguracionDeduccionId = table.Column<int>(type: "int", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Desde = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Hasta = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    Porcentaje = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MontoFijo = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Descripcion = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TramosISR", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VacacionesEmpleado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaInicio = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DiasSolicitados = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    DiasFeriados = table.Column<int>(type: "int", nullable: false),
                    DiasADescontar = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsuarioSolicitaId = table.Column<int>(type: "int", nullable: true),
                    Motivo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsuarioApruebaId = table.Column<int>(type: "int", nullable: true),
                    FechaAprobacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MotivoRechazo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaTomaReal = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Notas = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacacionesEmpleado", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ZonasEnvio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrecioSugerido = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Color = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activa = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZonasEnvio", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AperturasCaja_Numero",
                table: "AperturasCaja",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CierresCaja_Numero",
                table: "CierresCaja",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CierresContables_Anio_Mes",
                table: "CierresContables",
                columns: new[] { "Anio", "Mes" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConciliacionesBancarias_Numero",
                table: "ConciliacionesBancarias",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionDeducciones_Anio",
                table: "ConfiguracionDeducciones",
                column: "Anio",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_Codigo",
                table: "Empleados",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Envios_Numero",
                table: "Envios",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Nominas_Numero",
                table: "Nominas",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PagosNominaEmpleado_Numero",
                table: "PagosNominaEmpleado",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermisosEmpleado_Numero",
                table: "PermisosEmpleado",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanCuentas_Codigo",
                table: "PlanCuentas",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Repartidores_Codigo",
                table: "Repartidores",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rutas_Numero",
                table: "Rutas",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rutas_TrackingCode",
                table: "Rutas",
                column: "TrackingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VacacionesEmpleado_Numero",
                table: "VacacionesEmpleado",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZonasEnvio_Nombre",
                table: "ZonasEnvio",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdjuntosCierre");

            migrationBuilder.DropTable(
                name: "AdjuntosEmpleado");

            migrationBuilder.DropTable(
                name: "AlertasPersonalizadas");

            migrationBuilder.DropTable(
                name: "AperturasCaja");

            migrationBuilder.DropTable(
                name: "CierresCaja");

            migrationBuilder.DropTable(
                name: "CierresContables");

            migrationBuilder.DropTable(
                name: "ConciliacionesBancarias");

            migrationBuilder.DropTable(
                name: "ConciliacionesDetalle");

            migrationBuilder.DropTable(
                name: "ConfiguracionDeducciones");

            migrationBuilder.DropTable(
                name: "DetalleNominas");

            migrationBuilder.DropTable(
                name: "DistribucionesCierre");

            migrationBuilder.DropTable(
                name: "Empleados");

            migrationBuilder.DropTable(
                name: "Envios");

            migrationBuilder.DropTable(
                name: "ExpedientesEmpleado");

            migrationBuilder.DropTable(
                name: "Feriados");

            migrationBuilder.DropTable(
                name: "Nominas");

            migrationBuilder.DropTable(
                name: "PagosNominaEmpleado");

            migrationBuilder.DropTable(
                name: "PermisosEmpleado");

            migrationBuilder.DropTable(
                name: "PlanCuentas");

            migrationBuilder.DropTable(
                name: "Repartidores");

            migrationBuilder.DropTable(
                name: "Rutas");

            migrationBuilder.DropTable(
                name: "RutasHistorial");

            migrationBuilder.DropTable(
                name: "TiposDocumentoEmpleado");

            migrationBuilder.DropTable(
                name: "TramosISR");

            migrationBuilder.DropTable(
                name: "VacacionesEmpleado");

            migrationBuilder.DropTable(
                name: "ZonasEnvio");

            migrationBuilder.DropColumn(
                name: "EsCredito",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "FechaMovimiento",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "FechaVencimientoCredito",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MontoEnvio",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MovimientoCambioId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MovimientoIngresoId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MovimientoRegistrado",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "RequiereEnvio",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "SaldoPendiente",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "MontoEnvio",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "RequiereEnvio",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "Cambio",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "MontoRecibido",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "MontoEnvio",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "RequiereEnvio",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "MontoEnvio",
                table: "Cotizaciones");

            migrationBuilder.DropColumn(
                name: "RequiereEnvio",
                table: "Cotizaciones");

            migrationBuilder.DropColumn(
                name: "VentaId",
                table: "Cotizaciones");

            migrationBuilder.DropColumn(
                name: "PaisCodigo",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHAlertaAniversarios",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHAlertaContratosDias",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHAlertaContratosPorVencer",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHAlertaCumpleanios",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHAlertaDocumentosVencidos",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHAlertaFeriadosDias",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHAlertaFeriadosProximos",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHAlertaVacacionesDias",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHAlertaVacacionesProximas",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHAlertaValesPorVencer",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHAntiguedadMaxTabla",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHAplicaIHSS",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHAplicaISR",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHAplicaRAP",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHCargarFeriadosAuto",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHDiaPago1",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHDiaPago2",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHDiaSemanalPago",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHFrecuenciaPagoDefault",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHMetodoISRDefault",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHPorcentajeIHSS",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHPorcentajeRAP",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHTablaVacaciones",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "RHTopeIHSS",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "ZonaHoraria",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "PlanCuentaId",
                table: "CategoriasFinancieras");

            migrationBuilder.AlterColumn<string>(
                name: "MotivoRechazo",
                table: "ValesEmpleado",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
