using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kirkenta.Migrations
{
    /// <inheritdoc />
    public partial class NotaCreditoFiscal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CAI",
                table: "Devoluciones",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaLimiteEmision",
                table: "Devoluciones",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RangoFinal",
                table: "Devoluciones",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RangoInicial",
                table: "Devoluciones",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NotaCreditoCAI",
                table: "ConfiguracionEmpresa",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "NotaCreditoFechaLimite",
                table: "ConfiguracionEmpresa",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotaCreditoRangoFinal",
                table: "ConfiguracionEmpresa",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NotaCreditoRangoInicial",
                table: "ConfiguracionEmpresa",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "SiguienteNumeroNotaCredito",
                table: "ConfiguracionEmpresa",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CAI",
                table: "Devoluciones");

            migrationBuilder.DropColumn(
                name: "FechaLimiteEmision",
                table: "Devoluciones");

            migrationBuilder.DropColumn(
                name: "RangoFinal",
                table: "Devoluciones");

            migrationBuilder.DropColumn(
                name: "RangoInicial",
                table: "Devoluciones");

            migrationBuilder.DropColumn(
                name: "NotaCreditoCAI",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "NotaCreditoFechaLimite",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "NotaCreditoRangoFinal",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "NotaCreditoRangoInicial",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "SiguienteNumeroNotaCredito",
                table: "ConfiguracionEmpresa");
        }
    }
}
