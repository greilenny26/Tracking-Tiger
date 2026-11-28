using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tracking_Tiger.Migrations
{
    /// <inheritdoc />
    public partial class AgregarAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrosAuditoria",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Accion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Entidad = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EntidadId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ValorAnterior = table.Column<string>(type: "TEXT", maxLength: 600, nullable: false),
                    ValorNuevo = table.Column<string>(type: "TEXT", maxLength: 600, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosAuditoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosAuditoria_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_Entidad",
                table: "RegistrosAuditoria",
                column: "Entidad");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_Fecha",
                table: "RegistrosAuditoria",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_UsuarioId",
                table: "RegistrosAuditoria",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosAuditoria");
        }
    }
}
