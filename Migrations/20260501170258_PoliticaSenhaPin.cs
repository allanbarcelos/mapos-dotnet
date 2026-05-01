using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mapos_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class PoliticaSenhaPin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PinAlteradoEm",
                table: "usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PinPrimeiroUso",
                table: "usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PrimeiroAcesso",
                table: "usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SenhaAlteradaEm",
                table: "usuarios",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PinAlteradoEm",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "PinPrimeiroUso",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "PrimeiroAcesso",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "SenhaAlteradaEm",
                table: "usuarios");
        }
    }
}
