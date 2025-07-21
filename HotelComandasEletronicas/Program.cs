using Microsoft.EntityFrameworkCore;
using HotelComandasEletronicas.Data;
using HotelComandasEletronicas.Services;

var builder = WebApplication.CreateBuilder(args);

// Configura��o do Entity Framework
builder.Services.AddDbContext<ComandasDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configura��o de sess�o para autentica��o
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Timeout de 30 minutos
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// Registrar servi�os customizados
builder.Services.AddScoped<IUsuarioService, UsuarioService>();

// Configura��o de logging com Serilog (ser� implementado posteriormente)
// builder.Host.UseSerilog();

var app = builder.Build();

// Criar banco de dados e popular dados iniciais
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ComandasDbContext>();

    try
    {
        // Criar banco se n�o existir
        context.Database.EnsureCreated();

        // Popular dados iniciais
        context.PopularDadosIniciais();

        Console.WriteLine("? Banco de dados criado e populado com sucesso!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"? Erro ao criar banco: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Configurar uso de sess�o
app.UseSession();

app.UseAuthorization();

// Configura��o das rotas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Rotas espec�ficas para o sistema de comandas
app.MapControllerRoute(
    name: "consulta-cliente",
    pattern: "consulta",
    defaults: new { controller = "ConsultaCliente", action = "Index" });

app.MapControllerRoute(
    name: "lancamento",
    pattern: "lancamento",
    defaults: new { controller = "Lancamento", action = "Index" });

app.MapControllerRoute(
    name: "registro-hospede",
    pattern: "registro",
    defaults: new { controller = "RegistroHospede", action = "Index" });

Console.WriteLine("?? Sistema Hotel Comandas Eletr�nicas iniciado!");
Console.WriteLine("?? Acesso Gar�om: /lancamento");
Console.WriteLine("?? Consulta Cliente: /consulta");
Console.WriteLine("?? Registro H�spede: /registro");

app.Run();