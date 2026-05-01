using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace mapos_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categorias",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    categoria = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    cadastro = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<bool>(type: "boolean", nullable: false),
                    tipo = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clientes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    asaas_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    nome_cliente = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    sexo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    pessoa_fisica = table.Column<bool>(type: "boolean", nullable: false),
                    documento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    celular = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    senha = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    data_cadastro = table.Column<DateOnly>(type: "date", nullable: true),
                    rua = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: true),
                    numero = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    bairro = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    cidade = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    cep = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    contato = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    complemento = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    fornecedor = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clientes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "configuracoes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    config = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    valor = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracoes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    conta = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    banco = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    numero = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    saldo = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    cadastro = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<bool>(type: "boolean", nullable: false),
                    tipo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "documentos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    documento = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    file = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    url = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    cadastro = table.Column<DateOnly>(type: "date", nullable: true),
                    categoria = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    tipo = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    tamanho = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documentos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_queue",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    to_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    cc = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    bcc = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    headers = table.Column<string>(type: "text", nullable: true),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_queue", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "emitente",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    cnpj = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    ie = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    rua = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: true),
                    numero = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    bairro = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    cidade = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    uf = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    url_logo = table.Column<string>(type: "character varying(225)", maxLength: 225, nullable: true),
                    cep = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emitente", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "marcas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    marca = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cadastro = table.Column<DateOnly>(type: "date", nullable: true),
                    situacao = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marcas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissoes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    permissoes = table.Column<string>(type: "text", nullable: true),
                    situacao = table.Column<bool>(type: "boolean", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissoes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "resets_de_senha",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    data_expiracao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    token_utilizado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resets_de_senha", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "servicos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    descricao = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    preco = table.Column<decimal>(type: "numeric(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_servicos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "produtos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cod_de_barra = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: true),
                    descricao = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    unidade = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    preco_compra = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    preco_venda = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    estoque = table.Column<int>(type: "integer", nullable: false),
                    estoque_minimo = table.Column<int>(type: "integer", nullable: false),
                    saida = table.Column<bool>(type: "boolean", nullable: false),
                    entrada = table.Column<bool>(type: "boolean", nullable: false),
                    categoria_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_produtos", x => x.id);
                    table.ForeignKey(
                        name: "FK_produtos_categorias_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categorias",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "equipamentos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    equipamento = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    num_serie = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    modelo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    cor = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    descricao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    tensao = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    potencia = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    voltagem = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    data_fabricacao = table.Column<DateOnly>(type: "date", nullable: true),
                    marca_id = table.Column<int>(type: "integer", nullable: true),
                    cliente_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipamentos", x => x.id);
                    table.ForeignKey(
                        name: "FK_equipamentos_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_equipamentos_marcas_marca_id",
                        column: x => x.marca_id,
                        principalTable: "marcas",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    rg = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    cpf = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    cep = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    rua = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: true),
                    numero = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    bairro = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    cidade = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    senha = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    celular = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    situacao = table.Column<bool>(type: "boolean", nullable: false),
                    data_cadastro = table.Column<DateOnly>(type: "date", nullable: true),
                    permissao_id = table.Column<int>(type: "integer", nullable: false),
                    data_expiracao = table.Column<DateOnly>(type: "date", nullable: true),
                    url_image_user = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                    table.ForeignKey(
                        name: "FK_usuarios_permissoes_permissao_id",
                        column: x => x.permissao_id,
                        principalTable: "permissoes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "garantias",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    data_garantia = table.Column<DateOnly>(type: "date", nullable: true),
                    ref_garantia = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    texto_garantia = table.Column<string>(type: "text", nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_garantias", x => x.id);
                    table.ForeignKey(
                        name: "FK_garantias_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    tarefa = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    hora = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    usuario_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_logs_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "anexos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    arquivo = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    thumb = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    url = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    os_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_anexos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "anotacoes_os",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    anotacao = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    data_hora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    os_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_anotacoes_os", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cobrancas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    charge_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    conditional_discount_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    custom_id = table.Column<int>(type: "integer", nullable: true),
                    expire_at = table.Column<DateOnly>(type: "date", nullable: false),
                    message = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    payment_method = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    payment_url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    request_delivery_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    status = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    total = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    barcode = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    link = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    payment_gateway = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    payment = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    pdf = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    venda_id = table.Column<int>(type: "integer", nullable: true),
                    os_id = table.Column<int>(type: "integer", nullable: true),
                    cliente_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cobrancas", x => x.id);
                    table.ForeignKey(
                        name: "FK_cobrancas_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "equipamentos_os",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    defeito_declarado = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    defeito_encontrado = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    solucao = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    equipamento_id = table.Column<int>(type: "integer", nullable: false),
                    os_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipamentos_os", x => x.id);
                    table.ForeignKey(
                        name: "FK_equipamentos_os_equipamentos_equipamento_id",
                        column: x => x.equipamento_id,
                        principalTable: "equipamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "itens_de_vendas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sub_total = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    preco = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    venda_id = table.Column<int>(type: "integer", nullable: false),
                    produto_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itens_de_vendas", x => x.id);
                    table.ForeignKey(
                        name: "FK_itens_de_vendas_produtos_produto_id",
                        column: x => x.produto_id,
                        principalTable: "produtos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lancamentos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    descricao = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    desconto = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    valor_desconto = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    tipo_desconto = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    data_vencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    data_pagamento = table.Column<DateOnly>(type: "date", nullable: true),
                    baixado = table.Column<bool>(type: "boolean", nullable: false),
                    cliente_fornecedor = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    forma_pgto = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tipo = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    anexo = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    observacoes = table.Column<string>(type: "text", nullable: true),
                    cliente_id = table.Column<int>(type: "integer", nullable: true),
                    categoria_id = table.Column<int>(type: "integer", nullable: true),
                    conta_id = table.Column<int>(type: "integer", nullable: true),
                    venda_id = table.Column<int>(type: "integer", nullable: true),
                    usuario_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lancamentos", x => x.id);
                    table.ForeignKey(
                        name: "FK_lancamentos_categorias_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categorias",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_lancamentos_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_lancamentos_contas_conta_id",
                        column: x => x.conta_id,
                        principalTable: "contas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_lancamentos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "os",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    data_inicial = table.Column<DateOnly>(type: "date", nullable: true),
                    data_final = table.Column<DateOnly>(type: "date", nullable: true),
                    garantia = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    descricao_produto = table.Column<string>(type: "text", nullable: true),
                    defeito = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    observacoes = table.Column<string>(type: "text", nullable: true),
                    laudo_tecnico = table.Column<string>(type: "text", nullable: true),
                    valor_total = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    desconto = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    valor_desconto = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    tipo_desconto = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    cliente_id = table.Column<int>(type: "integer", nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    lancamento_id = table.Column<int>(type: "integer", nullable: true),
                    faturado = table.Column<bool>(type: "boolean", nullable: false),
                    garantia_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_os", x => x.id);
                    table.ForeignKey(
                        name: "FK_os_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_os_garantias_garantia_id",
                        column: x => x.garantia_id,
                        principalTable: "garantias",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_os_lancamentos_lancamento_id",
                        column: x => x.lancamento_id,
                        principalTable: "lancamentos",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_os_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vendas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    data_venda = table.Column<DateOnly>(type: "date", nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    desconto = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    valor_desconto = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    tipo_desconto = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    faturado = table.Column<bool>(type: "boolean", nullable: false),
                    observacoes = table.Column<string>(type: "text", nullable: true),
                    observacoes_cliente = table.Column<string>(type: "text", nullable: true),
                    cliente_id = table.Column<int>(type: "integer", nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
                    lancamento_id = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    garantia_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendas", x => x.id);
                    table.ForeignKey(
                        name: "FK_vendas_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_vendas_lancamentos_lancamento_id",
                        column: x => x.lancamento_id,
                        principalTable: "lancamentos",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_vendas_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "produtos_os",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    descricao = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    preco = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    os_id = table.Column<int>(type: "integer", nullable: false),
                    produto_id = table.Column<int>(type: "integer", nullable: false),
                    sub_total = table.Column<decimal>(type: "numeric(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_produtos_os", x => x.id);
                    table.ForeignKey(
                        name: "FK_produtos_os_os_os_id",
                        column: x => x.os_id,
                        principalTable: "os",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_produtos_os_produtos_produto_id",
                        column: x => x.produto_id,
                        principalTable: "produtos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "servicos_os",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    servico = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    quantidade = table.Column<double>(type: "double precision", nullable: false),
                    preco = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    os_id = table.Column<int>(type: "integer", nullable: false),
                    servico_id = table.Column<int>(type: "integer", nullable: false),
                    sub_total = table.Column<decimal>(type: "numeric(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_servicos_os", x => x.id);
                    table.ForeignKey(
                        name: "FK_servicos_os_os_os_id",
                        column: x => x.os_id,
                        principalTable: "os",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_servicos_os_servicos_servico_id",
                        column: x => x.servico_id,
                        principalTable: "servicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_anexos_os_id",
                table: "anexos",
                column: "os_id");

            migrationBuilder.CreateIndex(
                name: "IX_anotacoes_os_os_id",
                table: "anotacoes_os",
                column: "os_id");

            migrationBuilder.CreateIndex(
                name: "IX_cobrancas_cliente_id",
                table: "cobrancas",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_cobrancas_os_id",
                table: "cobrancas",
                column: "os_id");

            migrationBuilder.CreateIndex(
                name: "IX_cobrancas_venda_id",
                table: "cobrancas",
                column: "venda_id");

            migrationBuilder.CreateIndex(
                name: "IX_configuracoes_config",
                table: "configuracoes",
                column: "config",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_equipamentos_cliente_id",
                table: "equipamentos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipamentos_marca_id",
                table: "equipamentos",
                column: "marca_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipamentos_os_equipamento_id",
                table: "equipamentos_os",
                column: "equipamento_id");

            migrationBuilder.CreateIndex(
                name: "IX_equipamentos_os_os_id",
                table: "equipamentos_os",
                column: "os_id");

            migrationBuilder.CreateIndex(
                name: "IX_garantias_usuario_id",
                table: "garantias",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_itens_de_vendas_produto_id",
                table: "itens_de_vendas",
                column: "produto_id");

            migrationBuilder.CreateIndex(
                name: "IX_itens_de_vendas_venda_id",
                table: "itens_de_vendas",
                column: "venda_id");

            migrationBuilder.CreateIndex(
                name: "IX_lancamentos_categoria_id",
                table: "lancamentos",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_lancamentos_cliente_id",
                table: "lancamentos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_lancamentos_conta_id",
                table: "lancamentos",
                column: "conta_id");

            migrationBuilder.CreateIndex(
                name: "IX_lancamentos_usuario_id",
                table: "lancamentos",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_lancamentos_venda_id",
                table: "lancamentos",
                column: "venda_id");

            migrationBuilder.CreateIndex(
                name: "IX_logs_usuario_id",
                table: "logs",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_os_cliente_id",
                table: "os",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_os_garantia_id",
                table: "os",
                column: "garantia_id");

            migrationBuilder.CreateIndex(
                name: "IX_os_lancamento_id",
                table: "os",
                column: "lancamento_id");

            migrationBuilder.CreateIndex(
                name: "IX_os_usuario_id",
                table: "os",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_produtos_categoria_id",
                table: "produtos",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_produtos_os_os_id",
                table: "produtos_os",
                column: "os_id");

            migrationBuilder.CreateIndex(
                name: "IX_produtos_os_produto_id",
                table: "produtos_os",
                column: "produto_id");

            migrationBuilder.CreateIndex(
                name: "IX_servicos_os_os_id",
                table: "servicos_os",
                column: "os_id");

            migrationBuilder.CreateIndex(
                name: "IX_servicos_os_servico_id",
                table: "servicos_os",
                column: "servico_id");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_permissao_id",
                table: "usuarios",
                column: "permissao_id");

            migrationBuilder.CreateIndex(
                name: "IX_vendas_cliente_id",
                table: "vendas",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_vendas_lancamento_id",
                table: "vendas",
                column: "lancamento_id");

            migrationBuilder.CreateIndex(
                name: "IX_vendas_usuario_id",
                table: "vendas",
                column: "usuario_id");

            migrationBuilder.AddForeignKey(
                name: "FK_anexos_os_os_id",
                table: "anexos",
                column: "os_id",
                principalTable: "os",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_anotacoes_os_os_os_id",
                table: "anotacoes_os",
                column: "os_id",
                principalTable: "os",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_cobrancas_os_os_id",
                table: "cobrancas",
                column: "os_id",
                principalTable: "os",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_cobrancas_vendas_venda_id",
                table: "cobrancas",
                column: "venda_id",
                principalTable: "vendas",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_equipamentos_os_os_os_id",
                table: "equipamentos_os",
                column: "os_id",
                principalTable: "os",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_itens_de_vendas_vendas_venda_id",
                table: "itens_de_vendas",
                column: "venda_id",
                principalTable: "vendas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_lancamentos_vendas_venda_id",
                table: "lancamentos",
                column: "venda_id",
                principalTable: "vendas",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lancamentos_clientes_cliente_id",
                table: "lancamentos");

            migrationBuilder.DropForeignKey(
                name: "FK_vendas_clientes_cliente_id",
                table: "vendas");

            migrationBuilder.DropForeignKey(
                name: "FK_lancamentos_vendas_venda_id",
                table: "lancamentos");

            migrationBuilder.DropTable(
                name: "anexos");

            migrationBuilder.DropTable(
                name: "anotacoes_os");

            migrationBuilder.DropTable(
                name: "cobrancas");

            migrationBuilder.DropTable(
                name: "configuracoes");

            migrationBuilder.DropTable(
                name: "documentos");

            migrationBuilder.DropTable(
                name: "email_queue");

            migrationBuilder.DropTable(
                name: "emitente");

            migrationBuilder.DropTable(
                name: "equipamentos_os");

            migrationBuilder.DropTable(
                name: "itens_de_vendas");

            migrationBuilder.DropTable(
                name: "logs");

            migrationBuilder.DropTable(
                name: "produtos_os");

            migrationBuilder.DropTable(
                name: "resets_de_senha");

            migrationBuilder.DropTable(
                name: "servicos_os");

            migrationBuilder.DropTable(
                name: "equipamentos");

            migrationBuilder.DropTable(
                name: "produtos");

            migrationBuilder.DropTable(
                name: "os");

            migrationBuilder.DropTable(
                name: "servicos");

            migrationBuilder.DropTable(
                name: "marcas");

            migrationBuilder.DropTable(
                name: "garantias");

            migrationBuilder.DropTable(
                name: "clientes");

            migrationBuilder.DropTable(
                name: "vendas");

            migrationBuilder.DropTable(
                name: "lancamentos");

            migrationBuilder.DropTable(
                name: "categorias");

            migrationBuilder.DropTable(
                name: "contas");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "permissoes");
        }
    }
}
