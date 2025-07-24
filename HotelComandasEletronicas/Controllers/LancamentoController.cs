using Microsoft.AspNetCore.Mvc;
using HotelComandasEletronicas.Services;
using HotelComandasEletronicas.ViewModels;
using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.Controllers
{
    public class LancamentoController : BaseController
    {
        private readonly ILancamentoService _lancamentoService;
        private readonly IProdutoService _produtoService;
        private readonly IRegistroHospedeService _registroHospedeService;
        private readonly ILogger<LancamentoController> _logger;

        public LancamentoController(
            ILancamentoService lancamentoService,
            IProdutoService produtoService,
            IRegistroHospedeService registroHospedeService,
            ILogger<LancamentoController> logger)
        {
            _lancamentoService = lancamentoService;
            _produtoService = produtoService;
            _registroHospedeService = registroHospedeService;
            _logger = logger;
        }

        #region Views Principais

        /// <summary>
        /// Página principal de lançamento - Requer código ou login
        /// </summary>
        [HttpGet]
        [RequireCodigoOuLogin]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Buscar produtos ativos por categoria
                var produtos = await _produtoService.ListarAtivosAsync();
                var hospedes = await _registroHospedeService.ListarAtivosAsync();

                ViewBag.ProdutosPorCategoria = produtos.GroupBy(p => p.Categoria).ToDictionary(g => g.Key, g => g.ToList());
                ViewBag.HospedesAtivos = hospedes;
                ViewBag.UsuarioLancamento = UsuarioLogado?.Login ?? CodigoUsuarioAtual ?? "Desconhecido";

                LogarAcao("AcessoLancamento", "Acessou tela de lançamento de consumo");

                return View(new LancamentoViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar página de lançamento");
                DefinirMensagemErro("Erro ao carregar produtos. Tente novamente.");
                return RedirecionarParaHome();
            }
        }

        /// <summary>
        /// Página de histórico de lançamentos - Recepção/Supervisor
        /// </summary>
        [HttpGet]
        [RequireRecepcaoOuSupervisor]
        public async Task<IActionResult> Historico(DateTime? inicio = null, DateTime? fim = null, string? usuario = null)
        {
            try
            {
                inicio ??= DateTime.Today.AddDays(-7);
                fim ??= DateTime.Today.AddDays(1);

                List<LancamentoConsumo> lancamentos;

                if (!string.IsNullOrWhiteSpace(usuario))
                {
                    lancamentos = await _lancamentoService.GetConsumosPorUsuarioAsync(usuario);
                }
                else
                {
                    lancamentos = await _lancamentoService.GetConsumosPorPeriodoAsync(inicio.Value, fim.Value);
                }

                ViewBag.DataInicio = inicio;
                ViewBag.DataFim = fim;
                ViewBag.UsuarioFiltro = usuario;

                return View(lancamentos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar histórico de lançamentos");
                DefinirMensagemErro("Erro ao carregar histórico. Tente novamente.");
                return View(new List<LancamentoConsumo>());
            }
        }

        #endregion

        #region Operações de Lançamento

        /// <summary>
        /// Processar lançamento de consumo
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireCodigoOuLogin]
        public async Task<IActionResult> Lancar(LancamentoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Recarregar dados para retornar à view
                await RecarregarDadosView();
                return View("Index", model);
            }

            try
            {
                // Buscar produto e hóspede
                var produto = await _produtoService.BuscarPorIdAsync(model.ProdutoID);
                var hospede = await _registroHospedeService.BuscarPorIdAsync(model.RegistroHospedeID);

                if (produto == null || !produto.IsAtivo())
                {
                    ModelState.AddModelError("ProdutoID", "Produto não encontrado ou inativo.");
                    await RecarregarDadosView();
                    return View("Index", model);
                }

                if (hospede == null || !hospede.IsAtivo())
                {
                    ModelState.AddModelError("RegistroHospedeID", "Hóspede não encontrado ou inativo.");
                    await RecarregarDadosView();
                    return View("Index", model);
                }

                // Criar lançamento
                var lancamento = new LancamentoConsumo
                {
                    RegistroHospedeID = model.RegistroHospedeID,
                    ProdutoID = model.ProdutoID,
                    Quantidade = model.Quantidade,
                    ValorUnitario = produto.Valor,
                    ValorTotal = model.Quantidade * produto.Valor,
                    DataHoraLancamento = DateTime.Now,
                    CodigoUsuarioLancamento = CodigoUsuarioAtual ?? "00",
                    Status = "Ativo"
                };

                var sucesso = await _lancamentoService.RegistrarConsumoAsync(lancamento);

                if (sucesso)
                {
                    LogarAcao("LancarConsumo", 
                        $"Produto: {produto.Descricao} | Quantidade: {model.Quantidade} | " +
                        $"Quarto: {hospede.NumeroQuarto} | Valor: {lancamento.ValorTotal:C}",
                        "LANCAMENTOS_CONSUMO", lancamento.ID);

                    DefinirMensagemSucesso($"Consumo lançado com sucesso! {produto.Descricao} - Quarto {hospede.NumeroQuarto} - {lancamento.ValorTotal:C}");
                    
                    return RedirectToAction("Index");
                }
                else
                {
                    DefinirMensagemErro("Erro ao registrar consumo. Tente novamente.");
                    await RecarregarDadosView();
                    return View("Index", model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar lançamento");
                DefinirMensagemErro("Erro interno ao processar lançamento. Contate o suporte.");
                await RecarregarDadosView();
                return View("Index", model);
            }
        }

        /// <summary>
        /// Cancelar lançamento específico - Recepção/Supervisor
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRecepcaoOuSupervisor]
        public async Task<IActionResult> Cancelar(int id, string motivo = "")
        {
            try
            {
                var lancamento = await _lancamentoService.BuscarPorIdAsync(id);
                if (lancamento == null)
                {
                    return Json(new { sucesso = false, mensagem = "Lançamento não encontrado." });
                }

                if (!lancamento.IsAtivo())
                {
                    return Json(new { sucesso = false, mensagem = "Lançamento já foi cancelado." });
                }

                var usuarioCancelamento = CodigoUsuarioAtual ?? UsuarioLogado?.Login ?? "Sistema";
                var sucesso = await _lancamentoService.CancelarLancamentoAsync(id, motivo, usuarioCancelamento);

                if (sucesso)
                {
                    LogarAcao("CancelarLancamento", 
                        $"Lançamento ID: {id} cancelado. Motivo: {motivo}",
                        "LANCAMENTOS_CONSUMO", id);

                    return Json(new { sucesso = true, mensagem = "Lançamento cancelado com sucesso." });
                }
                else
                {
                    return Json(new { sucesso = false, mensagem = "Erro ao cancelar lançamento." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cancelar lançamento ID: {Id}", id);
                return Json(new { sucesso = false, mensagem = "Erro interno ao cancelar lançamento." });
            }
        }

        #endregion

        #region APIs AJAX

        /// <summary>
        /// Buscar produtos por categoria (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> BuscarProdutosPorCategoria(string categoria)
        {
            try
            {
                var produtos = await _produtoService.BuscarPorCategoriaAsync(categoria);

                var resultado = produtos.Select(p => new
                {
                    id = p.ID,
                    descricao = p.Descricao,
                    valor = p.Valor,
                    valorFormatado = p.FormatarValor()
                }).ToList();

                return Json(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar produtos por categoria: {Categoria}", categoria);
                return Json(new List<object>());
            }
        }

        /// <summary>
        /// Buscar hóspedes ativos (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> BuscarHospedes(string? termo = null)
        {
            try
            {
                var hospedes = await _registroHospedeService.ListarAtivosAsync();

                if (!string.IsNullOrWhiteSpace(termo))
                {
                    hospedes = hospedes.Where(h => 
                        h.NumeroQuarto.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                        h.NomeCliente.Contains(termo, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                var resultado = hospedes.Select(h => new
                {
                    id = h.ID,
                    numeroQuarto = h.NumeroQuarto,
                    nomeCliente = h.NomeCliente,
                    telefone = h.TelefoneCliente,
                    valorGasto = h.ValorGastoTotal,
                    display = $"Quarto {h.NumeroQuarto} - {h.NomeCliente}"
                }).ToList();

                return Json(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar hóspedes");
                return Json(new List<object>());
            }
        }

        /// <summary>
        /// Obter valor atual do produto (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObterValorProduto(int produtoId)
        {
            try
            {
                var produto = await _produtoService.BuscarPorIdAsync(produtoId);
                if (produto == null)
                {
                    return Json(new { erro = "Produto não encontrado" });
                }

                return Json(new
                {
                    valor = produto.Valor,
                    valorFormatado = produto.FormatarValor(),
                    descricao = produto.Descricao,
                    categoria = produto.Categoria
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter valor do produto ID: {ProdutoId}", produtoId);
                return Json(new { erro = "Erro interno" });
            }
        }

        /// <summary>
        /// Calcular valor total do lançamento (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CalcularTotal(int produtoId, decimal quantidade)
        {
            try
            {
                var produto = await _produtoService.BuscarPorIdAsync(produtoId);
                if (produto == null)
                {
                    return Json(new { erro = "Produto não encontrado" });
                }

                var total = produto.Valor * quantidade;

                return Json(new
                {
                    valorUnitario = produto.Valor,
                    quantidade = quantidade,
                    valorTotal = total,
                    valorTotalFormatado = total.ToString("C2")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao calcular total");
                return Json(new { erro = "Erro no cálculo" });
            }
        }

        #endregion

        #region Métodos Auxiliares

        /// <summary>
        /// Recarregar dados necessários para a view
        /// </summary>
        private async Task RecarregarDadosView()
        {
            var produtos = await _produtoService.ListarAtivosAsync();
            var hospedes = await _registroHospedeService.ListarAtivosAsync();

            ViewBag.ProdutosPorCategoria = produtos.GroupBy(p => p.Categoria).ToDictionary(g => g.Key, g => g.ToList());
            ViewBag.HospedesAtivos = hospedes;
            ViewBag.UsuarioLancamento = UsuarioLogado?.Login ?? CodigoUsuarioAtual ?? "Desconhecido";
        }

        #endregion
    }
}