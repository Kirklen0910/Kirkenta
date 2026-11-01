using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kirkenta.Migrations
{
    /// <inheritdoc />
    public partial class AddCogsToVentaYDetalle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CostoRegistrado",
                table: "Ventas",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "CostoTotalVendido",
                table: "Ventas",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MovimientoCostoId",
                table: "Ventas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostoTotalLinea",
                table: "DetalleVentas",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CostoUnitarioSnapshot",
                table: "DetalleVentas",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostoRegistrado",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "CostoTotalVendido",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MovimientoCostoId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "CostoTotalLinea",
                table: "DetalleVentas");

            migrationBuilder.DropColumn(
                name: "CostoUnitarioSnapshot",
                table: "DetalleVentas");
        }
    }
}
