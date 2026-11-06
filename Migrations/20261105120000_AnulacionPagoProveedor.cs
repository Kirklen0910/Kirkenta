using System;
using Kirkenta.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kirkenta.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261105120000_AnulacionPagoProveedor")]
    public partial class AnulacionPagoProveedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "PagosProveedor",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Activo");

            migrationBuilder.AddColumn<string>(
                name: "MotivoAnulacion",
                table: "PagosProveedor",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAnulacion",
                table: "PagosProveedor",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioAnuloId",
                table: "PagosProveedor",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Estado", table: "PagosProveedor");
            migrationBuilder.DropColumn(name: "MotivoAnulacion", table: "PagosProveedor");
            migrationBuilder.DropColumn(name: "FechaAnulacion", table: "PagosProveedor");
            migrationBuilder.DropColumn(name: "UsuarioAnuloId", table: "PagosProveedor");
        }
    }
}
