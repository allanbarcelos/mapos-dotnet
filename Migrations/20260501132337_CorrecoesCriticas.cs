using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mapos_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class CorrecoesCriticas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_lancamentos_venda_id",
                table: "lancamentos");

            migrationBuilder.CreateIndex(
                name: "IX_lancamentos_venda_id",
                table: "lancamentos",
                column: "venda_id",
                unique: true,
                filter: "venda_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_lancamentos_venda_id",
                table: "lancamentos");

            migrationBuilder.CreateIndex(
                name: "IX_lancamentos_venda_id",
                table: "lancamentos",
                column: "venda_id");
        }
    }
}
