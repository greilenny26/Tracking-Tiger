using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tracking_Tiger.Migrations
{
    /// <inheritdoc />
    public partial class CrearCorreoEnCola : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CorreosEnCola",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Destinatario = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    Asunto = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Cuerpo = table.Column<string>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Intentos = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UltimoError = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorreosEnCola", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CorreosEnCola_Estado",
                table: "CorreosEnCola",
                column: "Estado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CorreosEnCola");
        }
    }
}
