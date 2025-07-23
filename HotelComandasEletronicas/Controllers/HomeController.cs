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
                // Buscar estatísticas para o dashboard
                var usuarios = await _usuarioService.ListarAtivosAsync();
                var produtos = await _produtoService.ListarAtivosAsync();
                var hospedes = await _registroHospedeService.ListarAtivosAsync();

                // Estatísticas gerais
                ViewBag.TotalUsuarios = usuarios.Count;
                ViewBag.TotalProdutos = produtos.Count;
                ViewBag.TotalHospedes = hospedes.Count;
                ViewBag.TotalLancamentos = _context.LancamentosConsumo.Count(l => l.Status == "Ativo");

                // Dados para exibição
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

        /// <summary>
        /// Página de política de privacidade
        /// </summary>
        public IActionResult Privacy()
        {
            LogarAcao("AcessoPrivacidade", "Página de privacidade acessada");
            return View();
        }

        /// <summary>
        /// Página de erro
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError("Erro na aplicação - Request ID: {RequestId}", requestId);

            return View(new ErrorViewModel { RequestId = requestId });
        }

        #region Ferramentas de Teste (Desenvolvimento)

        /// <summary>
        /// Testar conexão com banco de dados
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> TestarConexao()
        {
            try
            {
                // Testar conexão básica
                var canConnect = await _context.Database.CanConnectAsync();

                if (canConnect)
                {
                    // Testar operações básicas
                    var totalUsuarios = await _usuarioService.ListarTodosAsync();
                    var totalProdutos = await _produtoService.ListarTodosAsync();
                    var totalHospedes = await _registroHospedeService.ListarTodosAsync();

                    var resultado = new
                    {
                        Status = "Sucesso",
                        Conexao = "OK",
                        Usuarios = totalUsuarios.Count,
                        Produtos = totalProdutos.Count,
                        Hospedes = totalHospedes.Count,
                        DataTeste = DateTime.Now
                    };

                    DefinirMensagemSucesso($"Conexão OK! Usuários: {totalUsuarios.Count}, Produtos: {totalProdutos.Count}, Hóspedes: {totalHospedes.Count}");
                    LogarAcao("TesteConexao", $"Teste realizado com sucesso: {resultado}");
                }
                else
                {
                    DefinirMensagemErro("Falha na conexão com o banco de dados!");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no teste de conexão");
                DefinirMensagemErro($"Erro no teste: {ex.Message}");
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Resetar dados iniciais do sistema
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ResetarDados()
        {
            try
            {
                // ATENÇÃO: Esta função deve ser removida em produção!
                if (!HttpContext.Request.Headers.ContainsKey("X-Desenvolvimento"))
                {
                    DefinirMensagemErro("Função disponível apenas em desenvolvimento.");
                    return RedirectToAction("Index");
                }

                // Limpar dados existentes (cuidado!)
                _context.LancamentosConsumo.RemoveRange(_context.LancamentosConsumo);
                _context.RegistrosHospede.RemoveRange(_context.RegistrosHospede);
                _context.Produtos.RemoveRange(_context.Produtos);
                _context.Usuarios.RemoveRange(_context.Usuarios);

                await _context.SaveChangesAsync();

                // Repopular dados iniciais
                _context.PopularDadosIniciais();

                DefinirMensagemSucesso("Dados resetados com sucesso! Sistema restaurado ao estado inicial.");
                LogarAcao("ResetDados", "Dados do sistema foram resetados");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao resetar dados");
                DefinirMensagemErro($"Erro ao resetar dados: {ex.Message}");
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Obter status detalhado do sistema (AJAX)
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
                        Lancamentos = await _context.LancamentosConsumo.CountAsync(),
                        Logs = await _context.LogsSistema.CountAsync()
                    },
                    Memoria = new
                    {
                        WorkingSet = Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024, // MB
                        PrivateMemory = Process.GetCurrentProcess().PrivateMemorySize64 / 1024 / 1024 // MB
                    },
                    Sistema = new
                    {
                        Versao = "v2.0",
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
        /// Gerar dados de teste para desenvolvimento
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GerarDadosTeste()
        {
            try
            {
                // Gerar alguns hóspedes fictícios
                var hospedesteste = new List<RegistroHospede>
                {
                    new RegistroHospede { NumeroQuarto = "101", NomeCliente = "João Silva", TelefoneCliente = "(95) 99999-1111", UsuarioRegistro = "anacclara01" },
                    new RegistroHospede { NumeroQuarto = "102", NomeCliente = "Maria Santos", TelefoneCliente = "(95) 99999-2222", UsuarioRegistro = "anacclara01" },
                    new RegistroHospede { NumeroQuarto = "205", NomeCliente = "Pedro Oliveira", TelefoneCliente = "(95) 99999-3333", UsuarioRegistro = "anacclara01" }
                };

                foreach (var hospede in hospedesteste)
                {
                    if (!await _registroHospedeService.QuartoJaExisteAsync(hospede.NumeroQuarto))
                    {
                        await _registroHospedeService.RegistrarHospedeAsync(hospede);
                    }
                }

                DefinirMensagemSucesso("Dados de teste gerados com sucesso!");
                LogarAcao("GerarDadosTeste", "Dados de teste criados");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar dados de teste");
                DefinirMensagemErro($"Erro ao gerar dados: {ex.Message}");
            }

            return RedirectToAction("Index");
        }

        #endregion

        #region APIs Públicas para Dashboard

        /// <summary>
        /// Obter estatísticas rápidas do sistema (AJAX)
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
                    hospedes = await _context.RegistrosHospede.CountAsync(h => h.IsAtivo()),
                    lancamentos = await _context.LancamentosConsumo.CountAsync(l => l.IsAtivo()),
                    valorTotal = await _context.RegistrosHospede
                        .Where(h => h.IsAtivo())
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

        /// <summary>
        /// Obter últimas atividades do sistema
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> UltimasAtividades()
        {
            try
            {
                var atividades = await _context.LogsSistema
                    .OrderByDescending(l => l.DataHora)
                    .Take(10)
                    .Select(l => new
                    {
                        l.DataHora,
                        l.CodigoUsuario,
                        l.Acao,
                        l.Tabela
                    })
                    .ToListAsync();

                return Json(atividades);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter últimas atividades");
                return Json(new { erro = "Erro ao carregar atividades" });
            }
        }

        #endregion
    }
}