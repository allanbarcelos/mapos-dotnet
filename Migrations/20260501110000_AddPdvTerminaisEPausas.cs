using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace mapos_dotnet.Migrations
{
    public partial class AddPdvTerminaisEPausas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pdv_terminais",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "disponivel"),
                    sessao_caixa_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pdv_terminais", x => x.id);
                    table.ForeignKey(
                        name: "FK_pdv_terminais_sessoes_caixa_sessao_caixa_id",
                        column: x => x.sessao_caixa_id,
                        principalTable: "sessoes_caixa",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "pdv_pausas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sessao_caixa_id = table.Column<int>(type: "integer", nullable: false),
                    iniciada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    retomada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pdv_pausas", x => x.id);
                    table.ForeignKey(
                        name: "FK_pdv_pausas_sessoes_caixa_sessao_caixa_id",
                        column: x => x.sessao_caixa_id,
                        principalTable: "sessoes_caixa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<int>(
                name: "pdv_terminal_id",
                table: "sessoes_caixa",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_pdv_terminais_sessao_caixa_id",
                table: "pdv_terminais",
                column: "sessao_caixa_id");

            migrationBuilder.CreateIndex(
                name: "IX_pdv_pausas_sessao_caixa_id",
                table: "pdv_pausas",
                column: "sessao_caixa_id");

            migrationBuilder.CreateIndex(
                name: "IX_sessoes_caixa_pdv_terminal_id",
                table: "sessoes_caixa",
                column: "pdv_terminal_id");

            migrationBuilder.AddForeignKey(
                name: "FK_sessoes_caixa_pdv_terminais_pdv_terminal_id",
                table: "sessoes_caixa",
                column: "pdv_terminal_id",
                principalTable: "pdv_terminais",
                principalColumn: "id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_sessoes_caixa_pdv_terminais_pdv_terminal_id", table: "sessoes_caixa");
            migrationBuilder.DropTable(name: "pdv_pausas");
            migrationBuilder.DropTable(name: "pdv_terminais");
            migrationBuilder.DropColumn(name: "pdv_terminal_id", table: "sessoes_caixa");
        }
    }
}
