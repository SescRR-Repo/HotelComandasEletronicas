using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelComandasEletronicas.Models;
using HotelComandasEletronicas.Services;
using HotelComandasEletronicas.Data;
using System.Diagnostics;

namespace HotelComandasEletronicas.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUsuarioService _usuarioService;
        private readonly IProdutoService _produtoService;
        private readonly IRegistroHospedeService _registroHospedeService;
        private readonly ComandasDbContext _context;

        public HomeController(
            ILogger<HomeController> logger,
            IUsuarioService usuarioService,
            IProdutoService produtoService,
            IRegistroHospedeService registroHospedeService,
            ComandasDbContext context)
        {
            _logger = logger;
            _usuarioService = usuarioService;
            _produtoService = produtoService;
            _registroHospedeService = registroHospedeService;
            _context = context;
        }

        /// <summary>
        /// Dashboard principal com estatísticas do sistema
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var usuarios = await _usuarioService.ListarAtivosAsync();
                var produtos = await _produtoService.ListarAtivosAsync();
                var hospedes = await _registroHospedeService.ListarAtivosAsync();

                ViewBag.TotalUsuarios = usuarios.Count;
                ViewBag.TotalProdutos = produtos.Count;
                ViewBag.TotalHospedes = hospedes.Count;
                ViewBag.TotalLancamentos = _context.LancamentosConsumo.Count(l => l.Status == "Ativo");

                ViewBag.UsuariosAtivos = usuarios.Take(5).Select(u => new
                {
                    u.Nome,
                    u.Perfil,
                    u.CodigoID
                }).ToList();

                ViewBag.ProdutosDisponiveis = produtos.Take(10).Select(p => new
                {
                    p.Descricao,
                    p.Categoria,
                    p.Valor
                }).ToList();

                LogarAcao("AcessoDashboard", "Dashboard principal carregado");

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar dashboard");
                DefinirMensagemErro("Erro ao carregar informações do sistema.");
                return View();
            }
        }

        public IActionResult Privacy()
        {
            LogarAcao("AcessoPrivacidade", "Página de privacidade acessada");
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError("Erro na aplicação - Request ID: {RequestId}", requestId);
            return View(new ErrorViewModel { RequestId = requestId });
        }

        /// <summary>
        /// Obter status detalhado do sistema (AJAX) - SEM LOGS
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> StatusSistema()
        {
            try
            {
                var status = new
                {
                    Timestamp = DateTime.Now,
                    Uptime = DateTime.Now.Subtract(Process.GetCurrentProcess().StartTime),
                    Banco = new
                    {
                        Conexao = await _context.Database.CanConnectAsync(),
                        Usuarios = await _context.Usuarios.CountAsync(),
                        Produtos = await _context.Produtos.CountAsync(),
                        Hospedes = await _context.RegistrosHospede.CountAsync(),
                        Lancamentos = await _context.LancamentosConsumo.CountAsync()
                        // REMOVIDO: Logs
                    },
                    Memoria = new
                    {
                        WorkingSet = Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024,
                        PrivateMemory = Process.GetCurrentProcess().PrivateMemorySize64 / 1024 / 1024
                    },
                    Sistema = new
                    {
                        Versao = "v2.0 - Otimizado",
                        Ambiente = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                        Servidor = Environment.MachineName,
                        Framework = Environment.Version.ToString()
                    }
                };

                return Json(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter status do sistema");
                return Json(new { erro = ex.Message });
            }
        }

        /// <summary>
        /// Estatísticas rápidas (AJAX) - SEM LOGS
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EstatisticasRapidas()
        {
            try
            {
                var stats = new
                {
                    usuarios = await _context.Usuarios.CountAsync(u => u.Status),
                    produtos = await _context.Produtos.CountAsync(p => p.Status),
                    hospedes = await _context.RegistrosHospede.CountAsync(h => h.Status == "Ativo"),
                    lancamentos = await _context.LancamentosConsumo.CountAsync(l => l.Status == "Ativo"),
                    valorTotal = await _context.RegistrosHospede
                        .Where(h => h.Status == "Ativo")
                        .SumAsync(h => h.ValorGastoTotal),
                    ultimaAtualizacao = DateTime.Now
                };

                return Json(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter estatísticas rápidas");
                return Json(new { erro = "Erro ao carregar estatísticas" });
            }
        }

        // REMOVIDO: UltimasAtividades() pois dependia de LogsSistema
    }
}