using Microsoft.EntityFrameworkCore;
using mapos_dotnet.Models;

namespace mapos_dotnet.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Permissao> Permissoes => Set<Permissao>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Conta> Contas => Set<Conta>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<Garantia> Garantias => Set<Garantia>();
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<Equipamento> Equipamentos => Set<Equipamento>();
    public DbSet<Os> Os => Set<Os>();
    public DbSet<ProdutoOs> ProdutosOs => Set<ProdutoOs>();
    public DbSet<ServicoOs> ServicosOs => Set<ServicoOs>();
    public DbSet<EquipamentoOs> EquipamentosOs => Set<EquipamentoOs>();
    public DbSet<Anexo> Anexos => Set<Anexo>();
    public DbSet<AnotacaoOs> AnotacoesOs => Set<AnotacaoOs>();
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<SessaoCaixa> SessoesCaixa => Set<SessaoCaixa>();
    public DbSet<ItemVenda> ItensVenda => Set<ItemVenda>();
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<Cobranca> Cobrancas => Set<Cobranca>();
    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<EmailQueue> EmailQueue => Set<EmailQueue>();
    public DbSet<Log> Logs => Set<Log>();
    public DbSet<Emitente> Emitente => Set<Emitente>();
    public DbSet<Configuracao> Configuracoes => Set<Configuracao>();
    public DbSet<ResetSenha> ResetsSenha => Set<ResetSenha>();
    public DbSet<PdvTerminal> PdvTerminais => Set<PdvTerminal>();
    public DbSet<PdvPausa> PdvPausas => Set<PdvPausa>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração de precisão decimal
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("numeric(10,2)");
        }

        // Cliente
        modelBuilder.Entity<Cliente>(e =>
        {
            e.ToTable("clientes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.NomeCliente).HasColumnName("nome_cliente").HasMaxLength(255).IsRequired();
            e.Property(x => x.AsaasId).HasColumnName("asaas_id").HasMaxLength(255);
            e.Property(x => x.Sexo).HasColumnName("sexo").HasMaxLength(20);
            e.Property(x => x.PessoaFisica).HasColumnName("pessoa_fisica");
            e.Property(x => x.Documento).HasColumnName("documento").HasMaxLength(20);
            e.Property(x => x.Telefone).HasColumnName("telefone").HasMaxLength(20);
            e.Property(x => x.Celular).HasColumnName("celular").HasMaxLength(20);
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(100);
            e.Property(x => x.Senha).HasColumnName("senha").HasMaxLength(200);
            e.Property(x => x.DataCadastro).HasColumnName("data_cadastro");
            e.Property(x => x.Rua).HasColumnName("rua").HasMaxLength(70);
            e.Property(x => x.Numero).HasColumnName("numero").HasMaxLength(15);
            e.Property(x => x.Bairro).HasColumnName("bairro").HasMaxLength(45);
            e.Property(x => x.Cidade).HasColumnName("cidade").HasMaxLength(45);
            e.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
            e.Property(x => x.Cep).HasColumnName("cep").HasMaxLength(20);
            e.Property(x => x.Contato).HasColumnName("contato").HasMaxLength(45);
            e.Property(x => x.Complemento).HasColumnName("complemento").HasMaxLength(45);
            e.Property(x => x.Fornecedor).HasColumnName("fornecedor");
            // M2: índice para busca por documento (CPF/CNPJ)
            e.HasIndex(x => x.Documento).HasDatabaseName("IX_clientes_documento");
        });

        // Permissao
        modelBuilder.Entity<Permissao>(e =>
        {
            e.ToTable("permissoes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(80).IsRequired();
            e.Property(x => x.Permissoes).HasColumnName("permissoes").HasColumnType("text");
            e.Property(x => x.Situacao).HasColumnName("situacao");
            e.Property(x => x.Data).HasColumnName("data");
        });

        // Usuario
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(80).IsRequired();
            e.Property(x => x.Rg).HasColumnName("rg").HasMaxLength(20);
            e.Property(x => x.Cpf).HasColumnName("cpf").HasMaxLength(20);
            e.Property(x => x.Cep).HasColumnName("cep").HasMaxLength(9);
            e.Property(x => x.Rua).HasColumnName("rua").HasMaxLength(70);
            e.Property(x => x.Numero).HasColumnName("numero").HasMaxLength(15);
            e.Property(x => x.Bairro).HasColumnName("bairro").HasMaxLength(45);
            e.Property(x => x.Cidade).HasColumnName("cidade").HasMaxLength(45);
            e.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(80).IsRequired();
            e.Property(x => x.Senha).HasColumnName("senha").HasMaxLength(200).IsRequired();
            e.Property(x => x.Telefone).HasColumnName("telefone").HasMaxLength(20);
            e.Property(x => x.Celular).HasColumnName("celular").HasMaxLength(20);
            e.Property(x => x.Situacao).HasColumnName("situacao");
            e.Property(x => x.DataCadastro).HasColumnName("data_cadastro");
            e.Property(x => x.PermissaoId).HasColumnName("permissao_id");
            e.Property(x => x.DataExpiracao).HasColumnName("data_expiracao");
            e.Property(x => x.UrlImageUser).HasColumnName("url_image_user").HasMaxLength(255);
            e.Property(x => x.FiscalPdv).HasColumnName("fiscal_pdv").HasDefaultValue(false);
            e.Property(x => x.OperadorCaixa).HasColumnName("operador_caixa").HasDefaultValue(false);
            // M3: aumentado de 40 para 500 para suportar o valor criptografado com AES
            e.Property(x => x.FiscalPdvCodigo).HasColumnName("fiscal_pdv_codigo").HasMaxLength(500);
            e.Property(x => x.FiscalPdvPin).HasColumnName("fiscal_pdv_pin").HasMaxLength(200);
            e.HasOne(x => x.Permissao).WithMany(x => x.Usuarios).HasForeignKey(x => x.PermissaoId);
            // M2: índice para busca por email (login)
            e.HasIndex(x => x.Email).HasDatabaseName("IX_usuarios_email");
        });

        // Categoria
        modelBuilder.Entity<Categoria>(e =>
        {
            e.ToTable("categorias");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nome).HasColumnName("categoria").HasMaxLength(80).IsRequired();
            e.Property(x => x.Cadastro).HasColumnName("cadastro");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(15);
        });

        // Conta
        modelBuilder.Entity<Conta>(e =>
        {
            e.ToTable("contas");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nome).HasColumnName("conta").HasMaxLength(45).IsRequired();
            e.Property(x => x.Banco).HasColumnName("banco").HasMaxLength(45);
            e.Property(x => x.NumeroAgencia).HasColumnName("numero").HasMaxLength(45);
            e.Property(x => x.Saldo).HasColumnName("saldo");
            e.Property(x => x.Cadastro).HasColumnName("cadastro");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(80);
        });

        // Produto
        modelBuilder.Entity<Produto>(e =>
        {
            e.ToTable("produtos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.CodDeBarra).HasColumnName("cod_de_barra").HasMaxLength(70);
            e.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(80).IsRequired();
            e.Property(x => x.Unidade).HasColumnName("unidade").HasMaxLength(10);
            e.Property(x => x.PrecoCompra).HasColumnName("preco_compra");
            e.Property(x => x.PrecoVenda).HasColumnName("preco_venda");
            e.Property(x => x.Estoque).HasColumnName("estoque");
            e.Property(x => x.EstoqueMinimo).HasColumnName("estoque_minimo");
            e.Property(x => x.Saida).HasColumnName("saida");
            e.Property(x => x.Entrada).HasColumnName("entrada");
            e.Property(x => x.CategoriaId).HasColumnName("categoria_id");
            e.HasOne(x => x.Categoria).WithMany(x => x.Produtos).HasForeignKey(x => x.CategoriaId);
        });

        // Servico
        modelBuilder.Entity<Servico>(e =>
        {
            e.ToTable("servicos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(45).IsRequired();
            e.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(45);
            e.Property(x => x.Preco).HasColumnName("preco");
        });

        // Garantia
        modelBuilder.Entity<Garantia>(e =>
        {
            e.ToTable("garantias");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.DataGarantia).HasColumnName("data_garantia");
            e.Property(x => x.RefGarantia).HasColumnName("ref_garantia").HasMaxLength(15);
            e.Property(x => x.TextoGarantia).HasColumnName("texto_garantia").HasColumnType("text");
            e.Property(x => x.UsuarioId).HasColumnName("usuario_id");
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
        });

        // Marca
        modelBuilder.Entity<Marca>(e =>
        {
            e.ToTable("marcas");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nome).HasColumnName("marca").HasMaxLength(100).IsRequired();
            e.Property(x => x.Cadastro).HasColumnName("cadastro");
            e.Property(x => x.Situacao).HasColumnName("situacao");
        });

        // Equipamento
        modelBuilder.Entity<Equipamento>(e =>
        {
            e.ToTable("equipamentos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nome).HasColumnName("equipamento").HasMaxLength(150).IsRequired();
            e.Property(x => x.NumSerie).HasColumnName("num_serie").HasMaxLength(80);
            e.Property(x => x.Modelo).HasColumnName("modelo").HasMaxLength(80);
            e.Property(x => x.Cor).HasColumnName("cor").HasMaxLength(45);
            e.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(150);
            e.Property(x => x.Tensao).HasColumnName("tensao").HasMaxLength(45);
            e.Property(x => x.Potencia).HasColumnName("potencia").HasMaxLength(45);
            e.Property(x => x.Voltagem).HasColumnName("voltagem").HasMaxLength(45);
            e.Property(x => x.DataFabricacao).HasColumnName("data_fabricacao");
            e.Property(x => x.MarcaId).HasColumnName("marca_id");
            e.Property(x => x.ClienteId).HasColumnName("cliente_id");
            e.HasOne(x => x.Marca).WithMany(x => x.Equipamentos).HasForeignKey(x => x.MarcaId);
            e.HasOne(x => x.Cliente).WithMany(x => x.Equipamentos).HasForeignKey(x => x.ClienteId);
        });

        // Os
        modelBuilder.Entity<Os>(e =>
        {
            e.ToTable("os");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.DataInicial).HasColumnName("data_inicial");
            e.Property(x => x.DataFinal).HasColumnName("data_final");
            e.Property(x => x.Garantia).HasColumnName("garantia").HasMaxLength(45);
            e.Property(x => x.DescricaoProduto).HasColumnName("descricao_produto").HasMaxLength(500);
            e.Property(x => x.Defeito).HasColumnName("defeito").HasMaxLength(2000);
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(45);
            e.Property(x => x.Observacoes).HasColumnName("observacoes").HasMaxLength(2000);
            e.Property(x => x.LaudoTecnico).HasColumnName("laudo_tecnico").HasMaxLength(2000);
            e.Property(x => x.ValorTotal).HasColumnName("valor_total");
            e.Property(x => x.Desconto).HasColumnName("desconto");
            e.Property(x => x.ValorDesconto).HasColumnName("valor_desconto");
            e.Property(x => x.TipoDesconto).HasColumnName("tipo_desconto").HasMaxLength(8);
            e.Property(x => x.ClienteId).HasColumnName("cliente_id");
            e.Property(x => x.UsuarioId).HasColumnName("usuario_id");
            e.Property(x => x.LancamentoId).HasColumnName("lancamento_id");
            e.Property(x => x.Faturado).HasColumnName("faturado");
            e.Property(x => x.GarantiaId).HasColumnName("garantia_id");
            e.HasOne(x => x.Cliente).WithMany(x => x.OsList).HasForeignKey(x => x.ClienteId);
            e.HasOne(x => x.Usuario).WithMany(x => x.OsList).HasForeignKey(x => x.UsuarioId);
            e.HasOne(x => x.Lancamento).WithMany(x => x.OsLancamentos).HasForeignKey(x => x.LancamentoId);
            e.HasOne(x => x.GarantiaTermo).WithMany(x => x.OsList).HasForeignKey(x => x.GarantiaId);
        });

        // ProdutoOs
        modelBuilder.Entity<ProdutoOs>(e =>
        {
            e.ToTable("produtos_os");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Quantidade).HasColumnName("quantidade");
            e.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(80);
            e.Property(x => x.Preco).HasColumnName("preco");
            e.Property(x => x.OsId).HasColumnName("os_id");
            e.Property(x => x.ProdutoId).HasColumnName("produto_id");
            e.Property(x => x.SubTotal).HasColumnName("sub_total");
            e.HasOne(x => x.Os).WithMany(x => x.ProdutosOs).HasForeignKey(x => x.OsId);
            e.HasOne(x => x.Produto).WithMany(x => x.ProdutosOs).HasForeignKey(x => x.ProdutoId);
        });

        // ServicoOs
        modelBuilder.Entity<ServicoOs>(e =>
        {
            e.ToTable("servicos_os");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Servico).HasColumnName("servico").HasMaxLength(80);
            e.Property(x => x.Quantidade).HasColumnName("quantidade");
            e.Property(x => x.Preco).HasColumnName("preco");
            e.Property(x => x.OsId).HasColumnName("os_id");
            e.Property(x => x.ServicoId).HasColumnName("servico_id");
            e.Property(x => x.SubTotal).HasColumnName("sub_total");
            e.HasOne(x => x.Os).WithMany(x => x.ServicosOs).HasForeignKey(x => x.OsId);
            e.HasOne(x => x.ServicoNav).WithMany(x => x.ServicosOs).HasForeignKey(x => x.ServicoId);
        });

        // EquipamentoOs
        modelBuilder.Entity<EquipamentoOs>(e =>
        {
            e.ToTable("equipamentos_os");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.DefeitoDeclarado).HasColumnName("defeito_declarado").HasMaxLength(200);
            e.Property(x => x.DefeitoEncontrado).HasColumnName("defeito_encontrado").HasMaxLength(200);
            e.Property(x => x.Solucao).HasColumnName("solucao").HasMaxLength(45);
            e.Property(x => x.EquipamentoId).HasColumnName("equipamento_id");
            e.Property(x => x.OsId).HasColumnName("os_id");
            e.HasOne(x => x.Equipamento).WithMany(x => x.EquipamentosOs).HasForeignKey(x => x.EquipamentoId);
            e.HasOne(x => x.Os).WithMany(x => x.EquipamentosOs).HasForeignKey(x => x.OsId);
        });

        // Anexo
        modelBuilder.Entity<Anexo>(e =>
        {
            e.ToTable("anexos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.NomeArquivo).HasColumnName("arquivo").HasMaxLength(45);
            e.Property(x => x.Thumb).HasColumnName("thumb").HasMaxLength(45);
            e.Property(x => x.Url).HasColumnName("url").HasMaxLength(300);
            e.Property(x => x.Path).HasColumnName("path").HasMaxLength(300);
            e.Property(x => x.OsId).HasColumnName("os_id");
            e.HasOne(x => x.Os).WithMany(x => x.Anexos).HasForeignKey(x => x.OsId);
        });

        // AnotacaoOs
        modelBuilder.Entity<AnotacaoOs>(e =>
        {
            e.ToTable("anotacoes_os");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Anotacao).HasColumnName("anotacao").HasMaxLength(255);
            e.Property(x => x.DataHora).HasColumnName("data_hora");
            e.Property(x => x.OsId).HasColumnName("os_id");
            e.HasOne(x => x.Os).WithMany(x => x.Anotacoes).HasForeignKey(x => x.OsId);
        });

        // SessaoCaixa
        modelBuilder.Entity<SessaoCaixa>(e =>
        {
            e.ToTable("sessoes_caixa");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.AbertoEm).HasColumnName("aberto_em");
            e.Property(x => x.FechadoEm).HasColumnName("fechado_em");
            e.Property(x => x.SaldoInicial).HasColumnName("saldo_inicial");
            e.Property(x => x.SaldoEsperado).HasColumnName("saldo_esperado");
            e.Property(x => x.SaldoInformado).HasColumnName("saldo_informado");
            e.Property(x => x.Diferenca).HasColumnName("diferenca");
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
            e.Property(x => x.Observacoes).HasColumnName("observacoes").HasMaxLength(2000);
            e.Property(x => x.OperadorId).HasColumnName("operador_id");
            e.Property(x => x.PdvTerminalId).HasColumnName("pdv_terminal_id");
            e.HasOne(x => x.Operador).WithMany().HasForeignKey(x => x.OperadorId);
            e.HasOne(x => x.PdvTerminal).WithMany(x => x.Sessoes).HasForeignKey(x => x.PdvTerminalId);
            // M2: índice composto para verificação de sessão aberta no PDV
            e.HasIndex(x => new { x.OperadorId, x.Status }).HasDatabaseName("IX_sessoes_caixa_operador_status");
        });

        // Venda
        modelBuilder.Entity<Venda>(e =>
        {
            e.ToTable("vendas");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.DataVenda).HasColumnName("data_venda");
            e.Property(x => x.ValorTotal).HasColumnName("valor_total");
            e.Property(x => x.Desconto).HasColumnName("desconto");
            e.Property(x => x.ValorDesconto).HasColumnName("valor_desconto");
            e.Property(x => x.TipoDesconto).HasColumnName("tipo_desconto").HasMaxLength(8);
            e.Property(x => x.Faturado).HasColumnName("faturado");
            e.Property(x => x.Observacoes).HasColumnName("observacoes").HasMaxLength(2000);
            e.Property(x => x.ObservacoesCliente).HasColumnName("observacoes_cliente").HasMaxLength(2000);
            e.Property(x => x.ClienteId).HasColumnName("cliente_id");
            e.Property(x => x.UsuarioId).HasColumnName("usuario_id");
            e.Property(x => x.LancamentoId).HasColumnName("lancamento_id");
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(45);
            e.Property(x => x.GarantiaId).HasColumnName("garantia_id");
            e.Property(x => x.SessaoCaixaId).HasColumnName("sessao_caixa_id");
            e.Property(x => x.FormaPagamento).HasColumnName("forma_pagamento").HasMaxLength(30);
            e.HasOne(x => x.Cliente).WithMany(x => x.Vendas).HasForeignKey(x => x.ClienteId);
            e.HasOne(x => x.Usuario).WithMany(x => x.Vendas).HasForeignKey(x => x.UsuarioId);
            e.HasOne(x => x.Lancamento).WithMany().HasForeignKey(x => x.LancamentoId);
            e.HasOne(x => x.SessaoCaixa).WithMany(x => x.Vendas).HasForeignKey(x => x.SessaoCaixaId);
        });

        // ItemVenda
        modelBuilder.Entity<ItemVenda>(e =>
        {
            e.ToTable("itens_de_vendas");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SubTotal).HasColumnName("sub_total");
            e.Property(x => x.Quantidade).HasColumnName("quantidade");
            e.Property(x => x.Preco).HasColumnName("preco");
            e.Property(x => x.VendaId).HasColumnName("venda_id");
            e.Property(x => x.ProdutoId).HasColumnName("produto_id");
            e.HasOne(x => x.Venda).WithMany(x => x.Itens).HasForeignKey(x => x.VendaId);
            e.HasOne(x => x.Produto).WithMany(x => x.ItensVenda).HasForeignKey(x => x.ProdutoId);
        });

        // Venda — M2: índice para filtros de data em relatórios
        modelBuilder.Entity<Venda>()
            .HasIndex(x => x.DataVenda).HasDatabaseName("IX_vendas_data_venda");

        // Lancamento
        modelBuilder.Entity<Lancamento>(e =>
        {
            e.ToTable("lancamentos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(255);
            e.Property(x => x.Valor).HasColumnName("valor");
            e.Property(x => x.Desconto).HasColumnName("desconto");
            e.Property(x => x.ValorDesconto).HasColumnName("valor_desconto");
            e.Property(x => x.TipoDesconto).HasColumnName("tipo_desconto").HasMaxLength(8);
            e.Property(x => x.DataVencimento).HasColumnName("data_vencimento");
            e.Property(x => x.DataPagamento).HasColumnName("data_pagamento");
            e.Property(x => x.Baixado).HasColumnName("baixado");
            e.Property(x => x.ClienteFornecedor).HasColumnName("cliente_fornecedor").HasMaxLength(255);
            e.Property(x => x.FormaPgto).HasColumnName("forma_pgto").HasMaxLength(100);
            e.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(45);
            e.Property(x => x.Anexo).HasColumnName("anexo").HasMaxLength(250);
            e.Property(x => x.Observacoes).HasColumnName("observacoes").HasMaxLength(2000);
            e.Property(x => x.ClienteId).HasColumnName("cliente_id");
            e.Property(x => x.CategoriaId).HasColumnName("categoria_id");
            e.Property(x => x.ContaId).HasColumnName("conta_id");
            e.Property(x => x.VendaId).HasColumnName("venda_id");
            e.Property(x => x.UsuarioId).HasColumnName("usuario_id");
            e.HasOne(x => x.Cliente).WithMany(x => x.Lancamentos).HasForeignKey(x => x.ClienteId);
            e.HasOne(x => x.Categoria).WithMany(x => x.Lancamentos).HasForeignKey(x => x.CategoriaId);
            e.HasOne(x => x.Conta).WithMany(x => x.Lancamentos).HasForeignKey(x => x.ContaId);
            e.HasOne(x => x.Usuario).WithMany(x => x.Lancamentos).HasForeignKey(x => x.UsuarioId);
            e.HasOne(x => x.Venda).WithMany().HasForeignKey(x => x.VendaId);
            // Garante que cada venda tenha no máximo um lançamento associado
            e.HasIndex(x => x.VendaId).IsUnique().HasFilter("venda_id IS NOT NULL");
        });

        // Cobranca
        modelBuilder.Entity<Cobranca>(e =>
        {
            e.ToTable("cobrancas");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ChargeId).HasColumnName("charge_id").HasMaxLength(255);
            e.Property(x => x.ConditionalDiscountDate).HasColumnName("conditional_discount_date");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.CustomId).HasColumnName("custom_id");
            e.Property(x => x.ExpireAt).HasColumnName("expire_at");
            e.Property(x => x.Message).HasColumnName("message").HasMaxLength(255);
            e.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasMaxLength(11);
            e.Property(x => x.PaymentUrl).HasColumnName("payment_url").HasMaxLength(255);
            e.Property(x => x.RequestDeliveryAddress).HasColumnName("request_delivery_address").HasMaxLength(64);
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(36);
            e.Property(x => x.Total).HasColumnName("total");
            e.Property(x => x.Barcode).HasColumnName("barcode").HasMaxLength(255);
            e.Property(x => x.Link).HasColumnName("link").HasMaxLength(255);
            e.Property(x => x.PaymentGateway).HasColumnName("payment_gateway").HasMaxLength(255);
            e.Property(x => x.Payment).HasColumnName("payment").HasMaxLength(64);
            e.Property(x => x.Pdf).HasColumnName("pdf").HasMaxLength(255);
            e.Property(x => x.VendaId).HasColumnName("venda_id");
            e.Property(x => x.OsId).HasColumnName("os_id");
            e.Property(x => x.ClienteId).HasColumnName("cliente_id");
            e.HasOne(x => x.Venda).WithMany(x => x.Cobrancas).HasForeignKey(x => x.VendaId);
            e.HasOne(x => x.Os).WithMany(x => x.Cobrancas).HasForeignKey(x => x.OsId);
            e.HasOne(x => x.Cliente).WithMany(x => x.Cobrancas).HasForeignKey(x => x.ClienteId);
        });

        // Documento
        modelBuilder.Entity<Documento>(e =>
        {
            e.ToTable("documentos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nome).HasColumnName("documento").HasMaxLength(70);
            e.Property(x => x.Descricao).HasColumnName("descricao").HasColumnType("text");
            e.Property(x => x.File).HasColumnName("file").HasMaxLength(100);
            e.Property(x => x.Path).HasColumnName("path").HasMaxLength(300);
            e.Property(x => x.Url).HasColumnName("url").HasMaxLength(300);
            e.Property(x => x.Cadastro).HasColumnName("cadastro");
            e.Property(x => x.Categoria).HasColumnName("categoria").HasMaxLength(80);
            e.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(15);
            e.Property(x => x.Tamanho).HasColumnName("tamanho").HasMaxLength(45);
        });

        // EmailQueue
        modelBuilder.Entity<EmailQueue>(e =>
        {
            e.ToTable("email_queue");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.To).HasColumnName("to_address").HasMaxLength(255);
            e.Property(x => x.Cc).HasColumnName("cc").HasMaxLength(255);
            e.Property(x => x.Bcc).HasColumnName("bcc").HasMaxLength(255);
            e.Property(x => x.Message).HasColumnName("message").HasColumnType("text");
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
            e.Property(x => x.Date).HasColumnName("date");
            e.Property(x => x.Headers).HasColumnName("headers").HasColumnType("text");
            e.Property(x => x.Subject).HasColumnName("subject").HasMaxLength(255);
        });

        // Log
        modelBuilder.Entity<Log>(e =>
        {
            e.ToTable("logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Usuario).HasColumnName("usuario").HasMaxLength(80);
            e.Property(x => x.Tarefa).HasColumnName("tarefa").HasMaxLength(100);
            e.Property(x => x.Data).HasColumnName("data");
            e.Property(x => x.Hora).HasColumnName("hora");
            e.Property(x => x.Ip).HasColumnName("ip").HasMaxLength(45);
            e.Property(x => x.UsuarioId).HasColumnName("usuario_id");
            e.HasOne(x => x.UsuarioNav).WithMany(x => x.Logs).HasForeignKey(x => x.UsuarioId);
        });

        // Emitente
        modelBuilder.Entity<Emitente>(e =>
        {
            e.ToTable("emitente");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(255);
            e.Property(x => x.Cnpj).HasColumnName("cnpj").HasMaxLength(45);
            e.Property(x => x.Ie).HasColumnName("ie").HasMaxLength(50);
            e.Property(x => x.Rua).HasColumnName("rua").HasMaxLength(70);
            e.Property(x => x.Numero).HasColumnName("numero").HasMaxLength(15);
            e.Property(x => x.Bairro).HasColumnName("bairro").HasMaxLength(45);
            e.Property(x => x.Cidade).HasColumnName("cidade").HasMaxLength(45);
            e.Property(x => x.Uf).HasColumnName("uf").HasMaxLength(20);
            e.Property(x => x.Telefone).HasColumnName("telefone").HasMaxLength(20);
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
            e.Property(x => x.UrlLogo).HasColumnName("url_logo").HasMaxLength(225);
            e.Property(x => x.Cep).HasColumnName("cep").HasMaxLength(20);
        });

        // Configuracao
        modelBuilder.Entity<Configuracao>(e =>
        {
            e.ToTable("configuracoes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Config).HasColumnName("config").HasMaxLength(50);
            e.Property(x => x.Valor).HasColumnName("valor").HasColumnType("text");
            e.HasIndex(x => x.Config).IsUnique();
        });

        // ResetSenha
        modelBuilder.Entity<ResetSenha>(e =>
        {
            e.ToTable("resets_de_senha");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(200);
            e.Property(x => x.Token).HasColumnName("token").HasMaxLength(255);
            e.Property(x => x.DataExpiracao).HasColumnName("data_expiracao");
            e.Property(x => x.TokenUtilizado).HasColumnName("token_utilizado");
        });

        // PdvTerminal
        modelBuilder.Entity<PdvTerminal>(e =>
        {
            e.ToTable("pdv_terminais");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(80).IsRequired();
            e.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(500);
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("disponivel");
            e.Property(x => x.SessaoCaixaId).HasColumnName("sessao_caixa_id");
            e.HasOne(x => x.SessaoCaixa).WithMany().HasForeignKey(x => x.SessaoCaixaId);
        });

        // PdvPausa
        modelBuilder.Entity<PdvPausa>(e =>
        {
            e.ToTable("pdv_pausas");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SessaoCaixaId).HasColumnName("sessao_caixa_id");
            e.Property(x => x.IniciadaEm).HasColumnName("iniciada_em");
            e.Property(x => x.RetomadaEm).HasColumnName("retomada_em");
            e.HasOne(x => x.Sessao).WithMany(x => x.Pausas).HasForeignKey(x => x.SessaoCaixaId);
        });
    }
}
