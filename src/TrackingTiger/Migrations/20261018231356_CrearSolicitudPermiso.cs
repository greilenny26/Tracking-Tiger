using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tracking_Tiger.Migrations
{
    /// <inheritdoc />
    public partial class CrearSolicitudPermiso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolicitudesPermiso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SolicitanteId = table.Column<int>(type: "INTEGER", nullable: false),
                    PermisoSolicitado = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AprobadorId = table.Column<int>(type: "INTEGER", nullable: true),
                    RechazadaPorId = table.Column<int>(type: "INTEGER", nullable: true),
                    MotivoRechazo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaResolucion = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesPermiso", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesPermiso_Usuarios_AprobadorId",
                        column: x => x.AprobadorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesPermiso_Usuarios_RechazadaPorId",
                        column: x => x.RechazadaPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesPermiso_Usuarios_SolicitanteId",
                        column: x => x.SolicitanteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesPermiso_AprobadorId",
                table: "SolicitudesPermiso",
                column: "AprobadorId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesPermiso_Estado",
                table: "SolicitudesPermiso",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesPermiso_RechazadaPorId",
                table: "SolicitudesPermiso",
                column: "RechazadaPorId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesPermiso_SolicitanteId",
                table: "SolicitudesPermiso",
                column: "SolicitanteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudesPermiso");
        }
    }
}
