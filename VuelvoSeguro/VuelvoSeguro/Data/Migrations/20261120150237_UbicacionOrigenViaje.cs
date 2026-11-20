using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VuelvoSeguro.Data.Migrations
{
    /// <inheritdoc />
    public partial class UbicacionOrigenViaje : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "OrigenLatitud",
                table: "Viajes",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OrigenLongitud",
                table: "Viajes",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrigenLatitud",
                table: "Viajes");

            migrationBuilder.DropColumn(
                name: "OrigenLongitud",
                table: "Viajes");
        }
    }
}
