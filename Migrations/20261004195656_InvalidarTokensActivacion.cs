using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tracking_Tiger.Migrations
{
    /// <inheritdoc />
    public partial class InvalidarTokensActivacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Invalidado",
                table: "TokensActivacion",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Invalidado",
                table: "TokensActivacion");
        }
    }
}
