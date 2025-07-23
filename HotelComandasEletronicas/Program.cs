using FluentValidation.AspNetCore;
using HotelComandasEletronicas.Data;
using HotelComandasEletronicas.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ===== CONFIGURAÇÃO DO SERILOG =====
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/hotel-comandas-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

builder.Host.UseSerilog();

// ===== CONFIGURAÇÃO DO ENTITY FRAMEWORK =====
builder.Services.AddDbContext<ComandasDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== CONFIGURAÇÃO DOS CONTROLLERS E VIEWS =====
builder.Services.AddControllersWithViews(options =>
{
    // Adicionar filtros globais se necessário
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

// ===== CONFIGURAÇÃO DA SESSÃO =====
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // Sessão expira em 1 hora
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "HotelComandas.Session";
});

// ===== CONFIGURAÇÃO DE CACHE =====
builder.Services.AddMemoryCache();

// ===== INJEÇÃO DE DEPENDÊNCIA DOS SERVIÇOS =====

// Serviços de Negócio (Scoped - uma instância por requisição)
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IProdutoService, ProdutoService>();
builder.Services.AddScoped<IRegistroHospedeService, RegistroHospedeService>();

// Serviços que serão criados posteriormente
builder.Services.AddScoped<ILancamentoService, LancamentoService>();
builder.Services.AddScoped<IConsultaClienteService, ConsultaClienteService>();
builder.Services.AddScoped<IRelatorioService, RelatorioService>();
builder.Services.AddScoped<ILogService, LogService>();

// ===== CONFIGURAÇÃO DE VALIDAÇÃO =====
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

// ===== CONFIGURAÇÃO DE AUTORIZAÇÃO =====
builder.Services.AddHttpContextAccessor();

// ===== CONFIGURAÇÃO DE ANTIFORGERY =====
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.SuppressXFrameOptionsHeader = false;
});

// ===== CONFIGURAÇÃO DE CULTURA BRASILEIRA =====
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "pt-BR" };
    options.SetDefaultCulture(supportedCultures[0])
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});

var app = builder.Build();

// ===== CONFIGURAÇÃO DO PIPELINE DE REQUISIÇÕES =====

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Middleware de redirecionamento HTTPS
app.UseHttpsRedirection();

// Middleware de arquivos estáticos
app.UseStaticFiles();

// Middleware de roteamento
app.UseRouting();

// Middleware de localização (cultura brasileira)
app.UseRequestLocalization();

// Middleware de sessão
app.UseSession();

// Middleware de autorização
app.UseAuthorization();

// ===== CONFIGURAÇÃO DE ROTAS =====
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Rotas específicas para funcionalidades principais
app.MapControllerRoute(
    name: "consulta",
    pattern: "consulta",
    defaults: new { controller = "ConsultaCliente", action = "Index" });

app.MapControllerRoute(
    name: "lancamento",
    pattern: "lancamento",
    defaults: new { controller = "Lancamento", action = "Index" });

app.MapControllerRoute(
    name: "registro",
    pattern: "registro",
    defaults: new { controller = "RegistroHospede", action = "Index" });

app.MapControllerRoute(
    name: "relatorios",
    pattern: "relatorios",
    defaults: new { controller = "Relatorio", action = "Index" });

// Rotas para API (AJAX)
app.MapControllerRoute(
    name: "api",
    pattern: "api/{controller}/{action}");

// ===== INICIALIZAÇÃO DO BANCO DE DADOS =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ComandasDbContext>();

        // Criar banco se não existir
        context.Database.EnsureCreated();

        // Popular dados iniciais
        context.PopularDadosIniciais();

        Log.Information("Banco de dados inicializado com sucesso");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Erro crítico ao inicializar banco de dados");
        throw;
    }
}

// ===== LOG DE INICIALIZAÇÃO =====
Log.Information("=== HOTEL COMANDAS ELETRÔNICAS v2.0 ===");
Log.Information("Sistema iniciado em {Environment} na porta {Port}",
    app.Environment.EnvironmentName,
    app.Urls.FirstOrDefault() ?? "localhost");
Log.Information("Banco de dados: {ConnectionString}",
    builder.Configuration.GetConnectionString("DefaultConnection"));

app.Run();

// ===== CONFIGURAÇÕES ADICIONAIS =====

/// <summary>
/// Extensão para configurar serviços específicos do hotel
/// </summary>
public static class ServiceExtensions
{
    public static IServiceCollection AddHotelServices(this IServiceCollection services)
    {
        // Configurações específicas do negócio
        services.Configure<HotelSettings>(options =>
        {
            options.NomeHotel = "Instância Ecológica do Tepequém";
            options.VersaoSistema = "v2.0";
            options.TimeoutSessao = TimeSpan.FromMinutes(60);
            options.MaxTentativasLogin = 3;
        });

        return services;
    }
}

/// <summary>
/// Configurações específicas do hotel
/// </summary>
public class HotelSettings
{
    public string NomeHotel { get; set; } = string.Empty;
    public string VersaoSistema { get; set; } = string.Empty;
    public TimeSpan TimeoutSessao { get; set; }
    public int MaxTentativasLogin { get; set; }
}

/// <summary>
/// Middleware personalizado para log de requisições
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            await _next(context);
        }
        finally
        {
            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                elapsedMs);
        }
    }
}

/// <summary>
/// Middleware para tratamento de erros específicos do sistema
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado na requisição {Method} {Path}",
                context.Request.Method, context.Request.Path);

            // Redirecionar para página de erro amigável
            if (!context.Response.HasStarted)
            {
                context.Response.Redirect("/Home/Error");
            }
        }
    }
}