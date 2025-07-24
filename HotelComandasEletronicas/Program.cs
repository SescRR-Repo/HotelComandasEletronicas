using Microsoft.EntityFrameworkCore;
using HotelComandasEletronicas.Data;
using HotelComandasEletronicas.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ===================================
// ?? CONFIGURA��O B�SICA DE LOGGING
// ===================================
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/hotel-comandas-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ===================================
// ?? CONFIGURA��O DO BANCO DE DADOS - SIMPLIFICADO
// ===================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=.\\SQLEXPRESS;Database=HotelComandasDB;Trusted_Connection=true;TrustServerCertificate=true;";

// �NICA CONFIGURA��O DO DBCONTEXT - SEM CONFLITOS
builder.Services.AddDbContext<ComandasDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// ===================================
// ?? SERVICES - CONFIGURA��O �NICA E LIMPA
// ===================================
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IProdutoService, ProdutoService>();
builder.Services.AddScoped<IRegistroHospedeService, RegistroHospedeService>();
builder.Services.AddScoped<ILancamentoService, LancamentoService>();
builder.Services.AddScoped<IConsultaClienteService, ConsultaClienteService>();
builder.Services.AddScoped<IRelatorioService, RelatorioService>();

// ===================================
// ?? CONFIGURA��O WEB
// ===================================
builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "HotelComandas.Session";
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.SuppressXFrameOptionsHeader = false;
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.AddHttpContextAccessor();

// ===================================
// ?? BUILD E CONFIGURA��O
// ===================================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseResponseCompression();
app.UseRouting();
app.UseSession();

// Log simples de requests
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Request: {Method} {Path}", context.Request.Method, context.Request.Path);
    await next();
});

// ===================================
// ??? ROTAS
// ===================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "lancamento",
    pattern: "lancamento/{action=Index}/{id?}",
    defaults: new { controller = "Lancamento" });

app.MapControllerRoute(
    name: "consulta",
    pattern: "consulta/{action=Index}/{id?}",
    defaults: new { controller = "ConsultaCliente" });

app.MapControllerRoute(
    name: "registro",
    pattern: "registro/{action=Index}/{id?}",
    defaults: new { controller = "RegistroHospede" });

// ===================================
// ?? INICIALIZA��O DO BANCO
// ===================================
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ComandasDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("?? Iniciando Hotel Comandas Eletr�nicas v2.0");

        await context.Database.MigrateAsync();
        logger.LogInformation("?? Migrations aplicadas");

        context.PopularDadosIniciais();
        logger.LogInformation("?? Dados iniciais populados");

        var totalUsuarios = await context.Usuarios.CountAsync();
        var totalProdutos = await context.Produtos.CountAsync();

        logger.LogInformation("?? Sistema iniciado: {Usuarios} usu�rios, {Produtos} produtos",
            totalUsuarios, totalProdutos);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "? Erro na inicializa��o");
        throw;
    }
}

// ===================================
// ?? INICIAR APLICA��O
// ===================================
try
{
    Log.Information("?? Sistema Hotel Comandas iniciado!");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "?? Erro fatal na aplica��o");
}
finally
{
    Log.CloseAndFlush();
}