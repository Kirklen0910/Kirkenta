using Kirkenta.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kirkenta.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261025180000_ExoneracionClientes")]
    public partial class ExoneracionClientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ----- Clientes -----
            migrationBuilder.AddColumn<bool>(
                name: "EsExonerado",
                table: "Clientes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ExoneradoConstancia",
                table: "Clientes",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ExoneradoOrdenCompraExenta",
                table: "Clientes",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ExoneradoRegistroSag",
                table: "Clientes",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // ----- Ventas -----
            migrationBuilder.AddColumn<bool>(
                name: "EsExonerada",
                table: "Ventas",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            // ----- Facturas -----
            migrationBuilder.AddColumn<bool>(
                name: "EsExonerada",
                table: "Facturas",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ExoneradoConstancia",
                table: "Facturas",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ExoneradoOrdenCompraExenta",
                table: "Facturas",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ExoneradoRegistroSag",
                table: "Facturas",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "EsExonerado", table: "Clientes");
            migrationBuilder.DropColumn(name: "ExoneradoConstancia", table: "Clientes");
            migrationBuilder.DropColumn(name: "ExoneradoOrdenCompraExenta", table: "Clientes");
            migrationBuilder.DropColumn(name: "ExoneradoRegistroSag", table: "Clientes");

            migrationBuilder.DropColumn(name: "EsExonerada", table: "Ventas");

            migrationBuilder.DropColumn(name: "EsExonerada", table: "Facturas");
            migrationBuilder.DropColumn(name: "ExoneradoConstancia", table: "Facturas");
            migrationBuilder.DropColumn(name: "ExoneradoOrdenCompraExenta", table: "Facturas");
            migrationBuilder.DropColumn(name: "ExoneradoRegistroSag", table: "Facturas");
        }
    }
}
