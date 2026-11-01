using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tracking_Tiger.Migrations
{
    /// <inheritdoc />
    public partial class ReemplazarAlertaFraudePorTarjeta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertasFraude");

            migrationBuilder.CreateTable(
                name: "Tarjetas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Titular = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UltimosCuatro = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    LimiteCredito = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Consumido = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tarjetas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HistorialEstadosTarjeta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TarjetaId = table.Column<int>(type: "INTEGER", nullable: false),
                    EstadoAnterior = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    EstadoNuevo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: true),
                    Nota = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialEstadosTarjeta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialEstadosTarjeta_Tarjetas_TarjetaId",
                        column: x => x.TarjetaId,
                        principalTable: "Tarjetas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialEstadosTarjeta_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transacciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TarjetaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Monto = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Comercio = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Aprobada = table.Column<bool>(type: "INTEGER", nullable: false),
                    MotivoDeclinacion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transacciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transacciones_Tarjetas_TarjetaId",
                        column: x => x.TarjetaId,
                        principalTable: "Tarjetas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstadosTarjeta_TarjetaId",
                table: "HistorialEstadosTarjeta",
                column: "TarjetaId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstadosTarjeta_UsuarioId",
                table: "HistorialEstadosTarjeta",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Transacciones_TarjetaId_Fecha",
                table: "Transacciones",
                columns: new[] { "TarjetaId", "Fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistorialEstadosTarjeta");

            migrationBuilder.DropTable(
                name: "Transacciones");

            migrationBuilder.DropTable(
                name: "Tarjetas");

            migrationBuilder.CreateTable(
                name: "AlertasFraude",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Comercio = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaResolucion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaTransaccion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Monto = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    NivelRiesgo = table.Column<int>(type: "INTEGER", nullable: false),
                    NumeroTarjetaEnmascarado = table.Column<string>(type: "TEXT", maxLength: 19, nullable: false),
                    Observacion = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertasFraude", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertasFraude_Estado",
                table: "AlertasFraude",
                column: "Estado");
        }
    }
}
