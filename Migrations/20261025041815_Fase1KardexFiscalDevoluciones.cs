using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kirkenta.Migrations
{
    /// <inheritdoc />
    public partial class Fase1KardexFiscalDevoluciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostoRevertido",
                table: "Devoluciones",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "FormaReembolso",
                table: "Devoluciones",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "MontoAplicadoSaldo",
                table: "Devoluciones",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoReembolsado",
                table: "Devoluciones",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MovimientoCostoId",
                table: "Devoluciones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MovimientoReembolsoId",
                table: "Devoluciones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReingresaInventario",
                table: "Devoluciones",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "CostoUnitario",
                table: "DetalleDevoluciones",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DetalleVentaId",
                table: "DetalleDevoluciones",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MovimientosInventario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Origen = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DocumentoId = table.Column<int>(type: "int", nullable: true),
                    DocumentoNumero = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cantidad = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    StockAnterior = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    StockPosterior = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: true),
                    Notas = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosInventario", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_ProductoId_Fecha",
                table: "MovimientosInventario",
                columns: new[] { "ProductoId", "Fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimientosInventario");

            migrationBuilder.DropColumn(
                name: "CostoRevertido",
                table: "Devoluciones");

            migrationBuilder.DropColumn(
                name: "FormaReembolso",
                table: "Devoluciones");

            migrationBuilder.DropColumn(
                name: "MontoAplicadoSaldo",
                table: "Devoluciones");

            migrationBuilder.DropColumn(
                name: "MontoReembolsado",
                table: "Devoluciones");

            migrationBuilder.DropColumn(
                name: "MovimientoCostoId",
                table: "Devoluciones");

            migrationBuilder.DropColumn(
                name: "MovimientoReembolsoId",
                table: "Devoluciones");

            migrationBuilder.DropColumn(
                name: "ReingresaInventario",
                table: "Devoluciones");

            migrationBuilder.DropColumn(
                name: "CostoUnitario",
                table: "DetalleDevoluciones");

            migrationBuilder.DropColumn(
                name: "DetalleVentaId",
                table: "DetalleDevoluciones");
        }
    }
}
