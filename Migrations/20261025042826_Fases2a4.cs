using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kirkenta.Migrations
{
    /// <inheritdoc />
    public partial class Fases2a4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MontoAplicadoSaldo",
                table: "DevolucionesProveedor",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoPorRecuperar",
                table: "DevolucionesProveedor",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DetalleOrdenCompraId",
                table: "DetalleDevolucionesProveedor",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MontoAplicadoSaldo",
                table: "DevolucionesProveedor");

            migrationBuilder.DropColumn(
                name: "MontoPorRecuperar",
                table: "DevolucionesProveedor");

            migrationBuilder.DropColumn(
                name: "DetalleOrdenCompraId",
                table: "DetalleDevolucionesProveedor");
        }
    }
}
