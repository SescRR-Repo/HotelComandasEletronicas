using Microsoft.EntityFrameworkCore;
using HotelComandasEletronicas.Data;
using HotelComandasEletronicas.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ===================================
// ?? CONFIGURA플O OTIMIZADA DE LOGGING (APENAS ARQUIVOS)
// ===================================
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/hotel-comandas-.txt", 
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,  // Manter apenas 30 dias
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// ===================================
// ?? CONFIGURA플O DO BANCO DE DADOS OTIMIZADA
// ===================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=.\\SQLEXPRESS;Database=HotelComandasDB;Trusted_Connection=true;TrustServerCertificate=true;";

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
// ?? SERVICES ESSENCIAIS
// ===================================
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IProdutoService, ProdutoService>();
builder.Services.AddScoped<IRegistroHospedeService, RegistroHospedeService>();
builder.Services.AddScoped<ILancamentoService, LancamentoService>();
builder.Services.AddScoped<IConsultaClienteService, ConsultaClienteService>();
// builder.Services.AddScoped<IRelatorioService, RelatorioService>(); // TEMPORARIAMENTE COMENTADO

// ===================================
// ?? CONFIGURA플O WEB
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
// ?? BUILD E CONFIGURA플O
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

// Log otimizado de requests (apenas em desenvolvimento)
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("?? {Method} {Path}", context.Request.Method, context.Request.Path);
        await next();
    });
}

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
    defaults: new { controller = "Registro" }); // Era "RegistroHospede"

// ===================================
// ?? INICIALIZA플O DO BANCO OTIMIZADA
// ===================================
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ComandasDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("?? Iniciando Hotel Comandas Eletr�nicas v2.0 - OTIMIZADO");

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
        logger.LogError(ex, "? Erro na inicializa豫o");
        throw;
    }
}

// ===================================
// ?? INICIAR APLICA플O
// ===================================
try
{
    Log.Information("?? Sistema Hotel Comandas OTIMIZADO iniciado!");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "?? Erro fatal na aplica豫o");
}
finally
{
    Log.CloseAndFlush();
}