using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tracking_Tiger.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMonedaTransaccion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MonedaOriginal",
                table: "Transacciones",
                type: "TEXT",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoOriginal",
                table: "Transacciones",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TasaCambio",
                table: "Transacciones",
                type: "TEXT",
                precision: 18,
                scale: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonedaOriginal",
                table: "Transacciones");

            migrationBuilder.DropColumn(
                name: "MontoOriginal",
                table: "Transacciones");

            migrationBuilder.DropColumn(
                name: "TasaCambio",
                table: "Transacciones");
        }
    }
}
