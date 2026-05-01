using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace mapos_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class AddSessaoCaixaAndPdvVenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "forma_pagamento",
                table: "vendas",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sessao_caixa_id",
                table: "vendas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sessoes_caixa",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    aberto_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fechado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    saldo_inicial = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    saldo_esperado = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    saldo_informado = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    diferenca = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    observacoes = table.Column<string>(type: "text", nullable: true),
                    operador_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessoes_caixa", x => x.id);
                    table.ForeignKey(
                        name: "FK_sessoes_caixa_usuarios_operador_id",
                        column: x => x.operador_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vendas_sessao_caixa_id",
                table: "vendas",
                column: "sessao_caixa_id");

            migrationBuilder.CreateIndex(
                name: "IX_sessoes_caixa_operador_id",
                table: "sessoes_caixa",
                column: "operador_id");

            migrationBuilder.AddForeignKey(
                name: "FK_vendas_sessoes_caixa_sessao_caixa_id",
                table: "vendas",
                column: "sessao_caixa_id",
                principalTable: "sessoes_caixa",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_vendas_sessoes_caixa_sessao_caixa_id",
                table: "vendas");

            migrationBuilder.DropTable(
                name: "sessoes_caixa");

            migrationBuilder.DropIndex(
                name: "IX_vendas_sessao_caixa_id",
                table: "vendas");

            migrationBuilder.DropColumn(
                name: "forma_pagamento",
                table: "vendas");

            migrationBuilder.DropColumn(
                name: "sessao_caixa_id",
                table: "vendas");
        }
    }
}
