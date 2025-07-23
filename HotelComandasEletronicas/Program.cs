using FluentValidation.AspNetCore;
using HotelComandasEletronicas.Data;
using HotelComandasEletronicas.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ===== CONFIGURA��O DO SERILOG =====
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/hotel-comandas-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

builder.Host.UseSerilog();

// ===== CONFIGURA��O DO ENTITY FRAMEWORK =====
builder.Services.AddDbContext<ComandasDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== CONFIGURA��O DOS CONTROLLERS E VIEWS =====
builder.Services.AddControllersWithViews(options =>
{
    // Adicionar filtros globais se necess�rio
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

// ===== CONFIGURA��O DA SESS�O =====
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // Sess�o expira em 1 hora
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "HotelComandas.Session";
});

// ===== CONFIGURA��O DE CACHE =====
builder.Services.AddMemoryCache();

// ===== INJE��O DE DEPEND�NCIA DOS SERVI�OS =====

// Servi�os de Neg�cio (Scoped - uma inst�ncia por requisi��o)
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IProdutoService, ProdutoService>();
builder.Services.AddScoped<IRegistroHospedeService, RegistroHospedeService>();

// Servi�os que ser�o criados posteriormente
builder.Services.AddScoped<ILancamentoService, LancamentoService>();
builder.Services.AddScoped<IConsultaClienteService, ConsultaClienteService>();
builder.Services.AddScoped<IRelatorioService, RelatorioService>();
builder.Services.AddScoped<ILogService, LogService>();

// ===== CONFIGURA��O DE VALIDA��O =====
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

// ===== CONFIGURA��O DE AUTORIZA��O =====
builder.Services.AddHttpContextAccessor();

// ===== CONFIGURA��O DE ANTIFORGERY =====
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.SuppressXFrameOptionsHeader = false;
});

// ===== CONFIGURA��O DE CULTURA BRASILEIRA =====
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "pt-BR" };
    options.SetDefaultCulture(supportedCultures[0])
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});

var app = builder.Build();

// ===== CONFIGURA��O DO PIPELINE DE REQUISI��ES =====

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

// Middleware de arquivos est�ticos
app.UseStaticFiles();

// Middleware de roteamento
app.UseRouting();

// Middleware de localiza��o (cultura brasileira)
app.UseRequestLocalization();

// Middleware de sess�o
app.UseSession();

// Middleware de autoriza��o
app.UseAuthorization();

// ===== CONFIGURA��O DE ROTAS =====
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Rotas espec�ficas para funcionalidades principais
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

// ===== INICIALIZA��O DO BANCO DE DADOS =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ComandasDbContext>();

        // Criar banco se n�o existir
        context.Database.EnsureCreated();

        // Popular dados iniciais
        context.PopularDadosIniciais();

        Log.Information("Banco de dados inicializado com sucesso");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Erro cr�tico ao inicializar banco de dados");
        throw;
    }
}

// ===== LOG DE INICIALIZA��O =====
Log.Information("=== HOTEL COMANDAS ELETR�NICAS v2.0 ===");
Log.Information("Sistema iniciado em {Environment} na porta {Port}",
    app.Environment.EnvironmentName,
    app.Urls.FirstOrDefault() ?? "localhost");
Log.Information("Banco de dados: {ConnectionString}",
    builder.Configuration.GetConnectionString("DefaultConnection"));

app.Run();

// ===== CONFIGURA��ES ADICIONAIS =====

/// <summary>
/// Extens�o para configurar servi�os espec�ficos do hotel
/// </summary>
public static class ServiceExtensions
{
    public static IServiceCollection AddHotelServices(this IServiceCollection services)
    {
        // Configura��es espec�ficas do neg�cio
        services.Configure<HotelSettings>(options =>
        {
            options.NomeHotel = "Inst�ncia Ecol�gica do Tepequ�m";
            options.VersaoSistema = "v2.0";
            options.TimeoutSessao = TimeSpan.FromMinutes(60);
            options.MaxTentativasLogin = 3;
        });

        return services;
    }
}

/// <summary>
/// Configura��es espec�ficas do hotel
/// </summary>
public class HotelSettings
{
    public string NomeHotel { get; set; } = string.Empty;
    public string VersaoSistema { get; set; } = string.Empty;
    public TimeSpan TimeoutSessao { get; set; }
    public int MaxTentativasLogin { get; set; }
}

/// <summary>
/// Middleware personalizado para log de requisi��es
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
/// Middleware para tratamento de erros espec�ficos do sistema
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
            _logger.LogError(ex, "Erro n�o tratado na requisi��o {Method} {Path}",
                context.Request.Method, context.Request.Path);

            // Redirecionar para p�gina de erro amig�vel
            if (!context.Response.HasStarted)
            {
                context.Response.Redirect("/Home/Error");
            }
        }
    }
}