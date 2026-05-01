using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mapos_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class IndicesEMaxLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_sessoes_caixa_operador_id",
                table: "sessoes_caixa");

            migrationBuilder.AlterColumn<string>(
                name: "observacoes_cliente",
                table: "vendas",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "observacoes",
                table: "vendas",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "fiscal_pdv_codigo",
                table: "usuarios",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "observacoes",
                table: "sessoes_caixa",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "descricao",
                table: "pdv_terminais",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "observacoes",
                table: "os",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "laudo_tecnico",
                table: "os",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "descricao_produto",
                table: "os",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "defeito",
                table: "os",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "observacoes",
                table: "lancamentos",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            // Converte total de varchar para numeric — USING necessário no PostgreSQL
            migrationBuilder.Sql(
                "ALTER TABLE cobrancas ALTER COLUMN total TYPE numeric(10,2) USING total::numeric(10,2)");
            migrationBuilder.AlterColumn<decimal>(
                name: "total",
                table: "cobrancas",
                type: "numeric(10,2)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(15)",
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendas_data_venda",
                table: "vendas",
                column: "data_venda");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_email",
                table: "usuarios",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_sessoes_caixa_operador_status",
                table: "sessoes_caixa",
                columns: new[] { "operador_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_clientes_documento",
                table: "clientes",
                column: "documento");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vendas_data_venda",
                table: "vendas");

            migrationBuilder.DropIndex(
                name: "IX_usuarios_email",
                table: "usuarios");

            migrationBuilder.DropIndex(
                name: "IX_sessoes_caixa_operador_status",
                table: "sessoes_caixa");

            migrationBuilder.DropIndex(
                name: "IX_clientes_documento",
                table: "clientes");

            migrationBuilder.AlterColumn<string>(
                name: "observacoes_cliente",
                table: "vendas",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "observacoes",
                table: "vendas",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "fiscal_pdv_codigo",
                table: "usuarios",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "observacoes",
                table: "sessoes_caixa",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "descricao",
                table: "pdv_terminais",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "observacoes",
                table: "os",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "laudo_tecnico",
                table: "os",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "descricao_produto",
                table: "os",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "defeito",
                table: "os",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "observacoes",
                table: "lancamentos",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "total",
                table: "cobrancas",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_sessoes_caixa_operador_id",
                table: "sessoes_caixa",
                column: "operador_id");
        }
    }
}
