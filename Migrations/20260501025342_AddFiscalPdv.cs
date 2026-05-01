using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mapos_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalPdv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "fiscal_pdv",
                table: "usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "fiscal_pdv_codigo",
                table: "usuarios",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fiscal_pdv_pin",
                table: "usuarios",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fiscal_pdv",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "fiscal_pdv_codigo",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "fiscal_pdv_pin",
                table: "usuarios");
        }
    }
}
