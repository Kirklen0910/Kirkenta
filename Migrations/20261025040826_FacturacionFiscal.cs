using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kirkenta.Migrations
{
    /// <inheritdoc />
    public partial class FacturacionFiscal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ZonaHoraria",
                table: "ConfiguracionEmpresa",
                type: "varchar(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(60)",
                oldMaxLength: 60,
                oldDefaultValue: "America/Tegucigalpa")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "RHTablaVacaciones",
                table: "ConfiguracionEmpresa",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200,
                oldDefaultValue: "1:10,2:12,3:15,4:20,5:20")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "RHMetodoISRDefault",
                table: "ConfiguracionEmpresa",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Acumulativo")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "RHFrecuenciaPagoDefault",
                table: "ConfiguracionEmpresa",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Mensual")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "PaisCodigo",
                table: "ConfiguracionEmpresa",
                type: "varchar(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(3)",
                oldMaxLength: 3,
                oldDefaultValue: "HN")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "FacturacionFiscalActiva",
                table: "ConfiguracionEmpresa",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "SiguienteNumeroFiscal",
                table: "ConfiguracionEmpresa",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FacturacionFiscalActiva",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "SiguienteNumeroFiscal",
                table: "ConfiguracionEmpresa");

            migrationBuilder.AlterColumn<string>(
                name: "ZonaHoraria",
                table: "ConfiguracionEmpresa",
                type: "varchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "America/Tegucigalpa",
                oldClrType: typeof(string),
                oldType: "varchar(60)",
                oldMaxLength: 60)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "RHTablaVacaciones",
                table: "ConfiguracionEmpresa",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "1:10,2:12,3:15,4:20,5:20",
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "RHMetodoISRDefault",
                table: "ConfiguracionEmpresa",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Acumulativo",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "RHFrecuenciaPagoDefault",
                table: "ConfiguracionEmpresa",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Mensual",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "PaisCodigo",
                table: "ConfiguracionEmpresa",
                type: "varchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "HN",
                oldClrType: typeof(string),
                oldType: "varchar(3)",
                oldMaxLength: 3)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
