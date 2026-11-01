using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kirkenta.Migrations
{
    /// <inheritdoc />
    public partial class FixDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // FIX 1: ConfiguracionEmpresa.PaisCodigo
            // ------------------------------------------------------------
            // El modelo tiene PaisCodigo = "HN" por defecto, pero la
            // migración AddCreditoFieldsToVenta la creó con
            // defaultValue: "" (string vacío). Los registros existentes
            // quedaron con "" en vez de "HN", lo que hace que
            // FeriadoSeeder busque feriados con PaisCodigo == "" y no
            // cargue ninguno para la empresa.
            //
            // También actualizamos el default a nivel de columna para
            // que los nuevos registros que se inserten sin especificar
            // PaisCodigo usen "HN".
            // ============================================================
            migrationBuilder.Sql(
                "UPDATE `ConfiguracionEmpresa` " +
                "SET `PaisCodigo` = 'HN' " +
                "WHERE `PaisCodigo` = '' OR `PaisCodigo` IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "PaisCodigo",
                table: "ConfiguracionEmpresa",
                type: "varchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "HN",
                oldClrType: typeof(string),
                oldType: "varchar(3)",
                oldMaxLength: 3,
                oldDefaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            // ============================================================
            // FIX 2: ConfiguracionEmpresa.RHMetodoISRDefault
            // ------------------------------------------------------------
            // El modelo tiene RHMetodoISRDefault = "Acumulativo" por
            // defecto, pero la migración la creó con defaultValue: "".
            // Los registros existentes quedaron con "" y el cálculo de
            // ISR caía en el else (método mensual simple) en vez del
            // acumulativo que corresponde por defecto en Honduras.
            // ============================================================
            migrationBuilder.Sql(
                "UPDATE `ConfiguracionEmpresa` " +
                "SET `RHMetodoISRDefault` = 'Acumulativo' " +
                "WHERE `RHMetodoISRDefault` = '' OR `RHMetodoISRDefault` IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "RHMetodoISRDefault",
                table: "ConfiguracionEmpresa",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Acumulativo",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            // ============================================================
            // FIX 3: ConfiguracionDeduccion.MetodoISR
            // ------------------------------------------------------------
            // Mismo problema que arriba. Los registros existentes de
            // ConfiguracionDeduccion quedaron con MetodoISR = "" y el
            // cálculo de ISR acumulativo no se aplicaba.
            // ============================================================
            migrationBuilder.Sql(
                "UPDATE `ConfiguracionDeducciones` " +
                "SET `MetodoISR` = 'Acumulativo' " +
                "WHERE `MetodoISR` = '' OR `MetodoISR` IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "MetodoISR",
                table: "ConfiguracionDeducciones",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Acumulativo",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            // ============================================================
            // FIX 4: ConfiguracionEmpresa.RHFrecuenciaPagoDefault
            // ------------------------------------------------------------
            // También quedó con defaultValue: "" en la migración original.
            // El modelo dice "Mensual". Los registros existentes quedaron
            // con "" y el cálculo de período de nómina caía en el default
            // del switch (Mensual), pero por consistencia lo dejamos bien.
            // ============================================================
            migrationBuilder.Sql(
                "UPDATE `ConfiguracionEmpresa` " +
                "SET `RHFrecuenciaPagoDefault` = 'Mensual' " +
                "WHERE `RHFrecuenciaPagoDefault` = '' OR `RHFrecuenciaPagoDefault` IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "RHFrecuenciaPagoDefault",
                table: "ConfiguracionEmpresa",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Mensual",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            // ============================================================
            // FIX 5: ConfiguracionEmpresa.ZonaHoraria
            // ------------------------------------------------------------
            // También quedó con defaultValue: "" en la migración original.
            // El modelo dice "America/Tegucigalpa".
            // ============================================================
            migrationBuilder.Sql(
                "UPDATE `ConfiguracionEmpresa` " +
                "SET `ZonaHoraria` = 'America/Tegucigalpa' " +
                "WHERE `ZonaHoraria` = '' OR `ZonaHoraria` IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "ZonaHoraria",
                table: "ConfiguracionEmpresa",
                type: "varchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "America/Tegucigalpa",
                oldClrType: typeof(string),
                oldType: "varchar(60)",
                oldMaxLength: 60,
                oldDefaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            // ============================================================
            // FIX 6: ConfiguracionEmpresa.RHTablaVacaciones
            // ------------------------------------------------------------
            // La migración original la creó con defaultValue: "". El modelo
            // dice "1:10,2:12,3:15,4:20,5:20". Los registros existentes
            // quedaron con "" y CalcularDiasVacacionesPorAntiguedad parsea
            // un string vacío → tabla vacía → devuelve 0 días siempre.
            // ============================================================
            migrationBuilder.Sql(
                "UPDATE `ConfiguracionEmpresa` " +
                "SET `RHTablaVacaciones` = '1:10,2:12,3:15,4:20,5:20' " +
                "WHERE `RHTablaVacaciones` = '' OR `RHTablaVacaciones` IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "RHTablaVacaciones",
                table: "ConfiguracionEmpresa",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "1:10,2:12,3:15,4:20,5:20",
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200,
                oldDefaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // Down: revertimos los defaults a "" (como estaban) y NO
            // tocamos los datos (no queremos dejar registros con "" de
            // nuevo, porque eso rompe la app). Los datos quedan con los
            // valores correctos aunque se revierta el esquema.
            // ============================================================

            migrationBuilder.AlterColumn<string>(
                name: "RHTablaVacaciones",
                table: "ConfiguracionEmpresa",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200,
                oldDefaultValue: "1:10,2:12,3:15,4:20,5:20")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ZonaHoraria",
                table: "ConfiguracionEmpresa",
                type: "varchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(60)",
                oldMaxLength: 60,
                oldDefaultValue: "America/Tegucigalpa")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "RHFrecuenciaPagoDefault",
                table: "ConfiguracionEmpresa",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Mensual")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "RHMetodoISRDefault",
                table: "ConfiguracionEmpresa",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Acumulativo")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "PaisCodigo",
                table: "ConfiguracionEmpresa",
                type: "varchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(3)",
                oldMaxLength: 3,
                oldDefaultValue: "HN")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "MetodoISR",
                table: "ConfiguracionDeducciones",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Acumulativo")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}