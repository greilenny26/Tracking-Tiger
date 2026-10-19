using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tracking_Tiger.Migrations
{
    /// <inheritdoc />
    public partial class CrearPermisoOtorgado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PermisosOtorgados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Operacion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SolicitudId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaOtorgamiento = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermisosOtorgados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermisosOtorgados_SolicitudesPermiso_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "SolicitudesPermiso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermisosOtorgados_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PermisosOtorgados_SolicitudId",
                table: "PermisosOtorgados",
                column: "SolicitudId");

            migrationBuilder.CreateIndex(
                name: "IX_PermisosOtorgados_UsuarioId_Operacion",
                table: "PermisosOtorgados",
                columns: new[] { "UsuarioId", "Operacion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PermisosOtorgados");
        }
    }
}
