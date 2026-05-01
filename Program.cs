using mapos_dotnet.Data;
using mapos_dotnet.Services;
using mapos_dotnet.Workers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath  = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    })
    .AddCookie("Mine", options =>
    {
        options.LoginPath        = "/Mine/Login";
        options.LogoutPath       = "/Mine/Sair";
        options.AccessDeniedPath = "/Mine/Login";
        options.ExpireTimeSpan   = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
        options.Cookie.Name      = ".MineAuth";
        options.Cookie.HttpOnly  = true;
        options.Cookie.SameSite  = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();
builder.Services.AddMemoryCache();

builder.Services.AddDataProtection();
builder.Services.AddSingleton<ILoginThrottleService, LoginThrottleService>();
builder.Services.AddScoped<IFiscalCodigoProtector, FiscalCodigoProtector>();

builder.Services.AddScoped<IPermissaoService, PermissaoService>();
builder.Services.AddScoped<IConfiguracaoService, ConfiguracaoService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IEmailQueueService, EmailQueueService>();
builder.Services.AddScoped<IMercadoPagoService, MercadoPagoService>();
builder.Services.AddScoped<EscPosService>();
builder.Services.AddScoped<IPdvPrinterService, PdvPrinterService>();
builder.Services.AddScoped<IPdvPointService, PdvPointService>();
builder.Services.AddHttpClient("MpPoint");

// Workers de e-mail (equivalente aos cron jobs do Map-OS PHP)
builder.Services.AddHostedService<EmailProcessWorker>(); // */2 * * * *
builder.Services.AddHostedService<EmailRetryWorker>();   // */5 * * * *

builder.Services.AddAntiforgery();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok("healthy"));

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

// Aplicar migrations automaticamente e seed inicial
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    // Seed configurações padrão
    if (!db.Configuracoes.Any())
    {
        db.Configuracoes.AddRange(
            new() { Config = "app_name",          Valor = "Map-OS" },
            new() { Config = "app_theme",          Valor = "white" },
            new() { Config = "per_page",           Valor = "10" },
            new() { Config = "os_notification",    Valor = "cliente" },
            new() { Config = "control_estoque",    Valor = "1" },
            new() { Config = "control_baixa",      Valor = "0" },
            new() { Config = "control_editos",     Valor = "1" },
            new() { Config = "control_datatable",  Valor = "1" },
            new() { Config = "pix_key",            Valor = "" },
            new() { Config = "control_edit_vendas",Valor = "1" },
            new() { Config = "email_automatico",        Valor = "1" },
            new() { Config = "email_process_interval", Valor = "2" },
            new() { Config = "email_retry_interval",   Valor = "5" },
            new() { Config = "control_2vias",      Valor = "0" },
            new() { Config = "os_status_list",     Valor = """["Aberto","Faturado","Negociação","Em Andamento","Orçamento","Finalizado","Cancelado","Aguardando Peças","Aprovado"]""" },
            new() { Config = "pdv_printer_host",    Valor = "" },
            new() { Config = "pdv_printer_port",    Valor = "9100" },
            new() { Config = "pdv_paper_cols",      Valor = "48" },
            new() { Config = "pdv_receipt_header",  Valor = "" },
            new() { Config = "pdv_receipt_footer",  Valor = "Obrigado pela preferencia!\nVolte sempre!" },
            new() { Config = "pdv_gerar_lancamento",Valor = "1" },
            new() { Config = "pdv_mp_access_token", Valor = "" },
            new() { Config = "pdv_mp_device_id",    Valor = "" }
        );
    }

    // Seed permissão admin
    if (!db.Permissoes.Any())
    {
        var permsAdmin = new Dictionary<string, int>
        {
            {"aCliente",1},{"eCliente",1},{"dCliente",1},{"vCliente",1},
            {"aProduto",1},{"eProduto",1},{"dProduto",1},{"vProduto",1},
            {"aServico",1},{"eServico",1},{"dServico",1},{"vServico",1},
            {"aOs",1},{"eOs",1},{"dOs",1},{"vOs",1},
            {"aVenda",1},{"eVenda",1},{"dVenda",1},{"vVenda",1},
            {"aGarantia",1},{"eGarantia",1},{"dGarantia",1},{"vGarantia",1},
            {"aArquivo",1},{"eArquivo",1},{"dArquivo",1},{"vArquivo",1},
            {"aLancamento",1},{"eLancamento",1},{"dLancamento",1},{"vLancamento",1},
            {"aCobranca",1},{"eCobranca",1},{"dCobranca",1},{"vCobranca",1},
            {"cUsuario",1},{"cEmitente",1},{"cPermissao",1},{"cBackup",1},
            {"cAuditoria",1},{"cEmail",1},{"cSistema",1},
            {"rCliente",1},{"rProduto",1},{"rServico",1},{"rOs",1},{"rVenda",1},{"rFinanceiro",1},
            {"vPdv",1},{"fPdv",1},
            {"vCategoria",1},{"aCategoria",1},{"eCategoria",1},{"dCategoria",1},
            {"vConta",1},{"aConta",1},{"eConta",1},{"dConta",1},
            {"vMarca",1},{"aMarca",1},{"eMarca",1},{"dMarca",1}
        };

        db.Permissoes.Add(new()
        {
            Nome = "Administrador",
            Permissoes = System.Text.Json.JsonSerializer.Serialize(permsAdmin),
            Situacao = true,
            Data = DateOnly.FromDateTime(DateTime.Today)
        });
    }

    // Seed usuário admin padrão
    if (!db.Usuarios.Any())
    {
        var adminPassword = Environment.GetEnvironmentVariable("MAPOS_ADMIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(adminPassword))
            throw new InvalidOperationException(
                "Variável de ambiente MAPOS_ADMIN_PASSWORD não definida. " +
                "Em Docker, configure o secret 'admin_password'. " +
                "Localmente, exporte a variável antes de executar.");

        db.Usuarios.Add(new()
        {
            Nome = "Administrador",
            Email = "admin@mail.com",
            Senha = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            Situacao = true,
            PermissaoId = 1,
            DataCadastro = DateOnly.FromDateTime(DateTime.Today),
            DataExpiracao = new DateOnly(3000, 1, 1)
        });
    }

    // Seed emitente padrão
    if (!db.Emitente.Any())
    {
        db.Emitente.Add(new() { Nome = "Minha Empresa" });
    }

    db.SaveChanges();
}

app.Run();
