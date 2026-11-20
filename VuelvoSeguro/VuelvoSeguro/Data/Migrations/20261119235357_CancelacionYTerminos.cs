using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VuelvoSeguro.Data.Migrations
{
    /// <inheritdoc />
    public partial class CancelacionYTerminos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CanceladoPorId",
                table: "Viajes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCancelacion",
                table: "Viajes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAceptacionTerminos",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Viajes_CanceladoPorId",
                table: "Viajes",
                column: "CanceladoPorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Viajes_Users_CanceladoPorId",
                table: "Viajes",
                column: "CanceladoPorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Viajes_Users_CanceladoPorId",
                table: "Viajes");

            migrationBuilder.DropIndex(
                name: "IX_Viajes_CanceladoPorId",
                table: "Viajes");

            migrationBuilder.DropColumn(
                name: "CanceladoPorId",
                table: "Viajes");

            migrationBuilder.DropColumn(
                name: "FechaCancelacion",
                table: "Viajes");

            migrationBuilder.DropColumn(
                name: "FechaAceptacionTerminos",
                table: "Users");
        }
    }
}
