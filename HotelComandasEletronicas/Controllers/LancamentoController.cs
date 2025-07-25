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
        private readonly IUsuarioService _usuarioService;
        private readonly ILogger<LancamentoController> _logger;

        public LancamentoController(
            ILancamentoService lancamentoService,
            IProdutoService produtoService,
            IRegistroHospedeService registroHospedeService,
            IUsuarioService usuarioService,
            ILogger<LancamentoController> logger)
        {
            _lancamentoService = lancamentoService;
            _produtoService = produtoService;
            _registroHospedeService = registroHospedeService;
            _usuarioService = usuarioService;
            _logger = logger;
        }

        #region Views Principais

        /// <summary>
        /// P�gina principal de lan�amento - ACESSO LIVRE (sem autentica��o pr�via)
        /// Sistema de Comanda Eletr�nica - Pr�-lan�amentos com confirma��o final
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Buscar produtos ativos por categoria
                var produtos = await _produtoService.ListarAtivosAsync();
                var hospedes = await _registroHospedeService.ListarAtivosAsync();

                // Preparar dados para JavaScript
                ViewBag.ProdutosPorCategoria = produtos.GroupBy(p => p.Categoria).ToDictionary(g => g.Key, g => g.ToList());
                ViewBag.HospedesAtivos = hospedes;
                
                // NOVO: Adicionar produtos serializados para JavaScript
                ViewBag.ProdutosTodos = produtos.Select(p => new
                {
                    id = p.ID,
                    descricao = p.Descricao,
                    categoria = p.Categoria,
                    valor = p.Valor,
                    valorFormatado = p.FormatarValor()
                }).ToList();

                // Log sem usu�rio espec�fico - apenas acesso � comanda
                _logger.LogInformation("Acesso � Comanda Eletr�nica (pr�-lan�amentos) - IP: {IP}", 
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");

                return View(new ComandaLancamentoViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar sistema de comanda");
                DefinirMensagemErro("Erro ao carregar produtos. Tente novamente.");
                return RedirecionarParaHome();
            }
        }

        /// <summary>
        /// Processar comanda completa - COM VALIDA��O DE C�DIGO DO GAR�OM
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessarComanda([FromBody] ProcessarComandaViewModel model)
        {
            try
            {
                _logger.LogInformation("Iniciando processamento de comanda - Cliente ID: {ClienteId}, Gar�om: {Garcom}", 
                    model.RegistroHospedeID, model.CodigoGarcom);

                // Validar modelo
                if (!ModelState.IsValid || !model.ItensPedido.Any())
                {
                    _logger.LogWarning("Dados inv�lidos na comanda - ModelState: {ModelState}", 
                        string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    return Json(new { sucesso = false, mensagem = "Dados inv�lidos ou comanda vazia." });
                }

                // Validar c?digo do gar?om (FORMATO)
                if (string.IsNullOrWhiteSpace(model.CodigoGarcom) || model.CodigoGarcom.Length != 2)
                {
                    return Json(new { sucesso = false, mensagem = "C�digo do gar�om inv�lido. Deve ter 2 d�gitos." });
                }

                // ? NOVA VALIDA��O: Verificar se o c�digo do gar�om existe no sistema
                var usuarioValidacao = await _usuarioService.ValidarCodigoAsync(model.CodigoGarcom);

                if (usuarioValidacao == null)
                {
                    _logger.LogWarning("Tentativa de usar c�digo de gar�om inexistente: {CodigoGarcom}", model.CodigoGarcom);
                    return Json(new { sucesso = false, mensagem = $"C�digo '{model.CodigoGarcom}' n�o est� cadastrado no sistema. Verifique com a recep��o." });
                }

                // Log da valida��o bem-sucedida
                _logger.LogInformation("C�digo do gar�om validado: {CodigoGarcom} - {NomeUsuario} ({Perfil})", 
                    model.CodigoGarcom, usuarioValidacao.Nome, usuarioValidacao.Perfil);

                // Buscar e validar h?spede
                var hospede = await _registroHospedeService.BuscarPorIdAsync(model.RegistroHospedeID);
                if (hospede == null || !hospede.IsAtivo())
                {
                    return Json(new { sucesso = false, mensagem = "H�spede n�o encontrado ou inativo." });
                }

                var lancamentosProcessados = new List<LancamentoConsumo>();
                decimal valorTotalComanda = 0;

                // Processar cada item da comanda
                foreach (var item in model.ItensPedido)
                {
                    // Buscar produto
                    var produto = await _produtoService.BuscarPorIdAsync(item.ProdutoID);
                    if (produto == null || !produto.IsAtivo())
                    {
                        return Json(new { sucesso = false, mensagem = $"Produto ID {item.ProdutoID} n�o encontrado ou inativo." });
                    }

                    // Criar lan�amento
                    var lancamento = new LancamentoConsumo
                    {
                        RegistroHospedeID = model.RegistroHospedeID,
                        ProdutoID = item.ProdutoID,
                        Quantidade = item.Quantidade,
                        ValorUnitario = produto.Valor,
                        ValorTotal = item.Quantidade * produto.Valor,
                        DataHoraLancamento = DateTime.Now,
                        CodigoUsuarioLancamento = model.CodigoGarcom,
                        Status = "Ativo"
                    };

                    // Registrar lan?amento
                    var sucesso = await _lancamentoService.RegistrarConsumoAsync(lancamento);
                    if (!sucesso)
                    {
                        _logger.LogError("Erro ao registrar produto {Produto} na comanda", produto.Descricao);
                        return Json(new { sucesso = false, mensagem = $"Erro ao registrar {produto.Descricao} na comanda." });
                    }

                    lancamentosProcessados.Add(lancamento);
                    valorTotalComanda += lancamento.ValorTotal;
                }

                // Log da comanda processada
                LogarAcao("ProcessarComanda", 
                    $"Comanda processada - Quarto: {hospede.NumeroQuarto} | " +
                    $"Itens: {lancamentosProcessados.Count} | Total: {valorTotalComanda:C} | " +
                    $"Gar�om: {model.CodigoGarcom} ({usuarioValidacao.Nome}) | " +
                    $"Pedidos: {string.Join(", ", lancamentosProcessados.Select(l => l.Produto?.Descricao ?? "N/A"))}",
                    "LANCAMENTOS_CONSUMO");

                _logger.LogInformation("Comanda processada com sucesso - {Total} itens, valor: {Valor}, Gar�om: {NomeGarcom}", 
                    lancamentosProcessados.Count, valorTotalComanda, usuarioValidacao.Nome);

                return Json(new { 
                    sucesso = true, 
                    mensagem = $"Comanda processada com sucesso! {lancamentosProcessados.Count} pedidos registrados para o quarto {hospede.NumeroQuarto}.",
                    totalItens = lancamentosProcessados.Count,
                    valorTotal = valorTotalComanda.ToString("C2"),
                    numeroQuarto = hospede.NumeroQuarto,
                    nomeCliente = hospede.NomeCliente,
                    codigoGarcom = model.CodigoGarcom,
                    nomeGarcom = usuarioValidacao.Nome
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar comanda");
                return Json(new { sucesso = false, mensagem = "Erro interno ao processar comanda. Contate o suporte." });
            }
        }

        /// <summary>
        /// P�gina de hist�rico de lan�amentos - Recep��o/Supervisor
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
                _logger.LogError(ex, "Erro ao carregar hist�rico de lan�amentos");
                DefinirMensagemErro("Erro ao carregar hist�rico. Tente novamente.");
                return View(new List<LancamentoConsumo>());
            }
        }

        #endregion

        #region APIs AJAX

        /// <summary>
        /// Buscar produtos (AJAX) - SEM FILTRO INICIAL
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> BuscarProdutos(string? termo = null)
        {
            try
            {
                var produtos = await _produtoService.ListarAtivosAsync();

                if (!string.IsNullOrWhiteSpace(termo))
                {
                    produtos = produtos.Where(p => 
                        p.Descricao.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                        p.Categoria.Contains(termo, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                var resultado = produtos.Select(p => new
                {
                    id = p.ID,
                    descricao = p.Descricao,
                    categoria = p.Categoria,
                    valor = p.Valor,
                    valorFormatado = p.FormatarValor()
                }).ToList();

                return Json(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar produtos");
                return Json(new List<object>());
            }
        }

        /// <summary>
        /// Buscar h�spedes por termo (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> BuscarHospedes(string? termo = null)
        {
            try
            {
                var hospedes = await _registroHospedeService.ListarAtivosAsync();

                if (!string.IsNullOrWhiteSpace(termo))
                {
                    // Busca inteligente conforme wireframe
                    hospedes = hospedes.Where(h => 
                        h.NumeroQuarto.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                        h.NomeCliente.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                        h.TelefoneCliente.Contains(termo, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                var resultado = hospedes.Select(h => new
                {
                    id = h.ID,
                    numeroQuarto = h.NumeroQuarto,
                    nomeCliente = h.NomeCliente,
                    telefone = h.TelefoneCliente,
                    valorGasto = h.ValorGastoTotal,
                    valorGastoFormatado = h.ValorGastoTotal.ToString("C2"),
                    display = $"{h.NomeCliente} (Quarto {h.NumeroQuarto})"
                }).ToList();

                return Json(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar h�spedes");
                return Json(new List<object>());
            }
        }

        /// <summary>
        /// Cancelar lan�amento espec�fico - Recep��o/Supervisor
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
                    return Json(new { sucesso = false, mensagem = "Lan�amento n�o encontrado." });
                }

                if (!lancamento.IsAtivo())
                {
                    return Json(new { sucesso = false, mensagem = "Lan�amento j� foi cancelado." });
                }

                var usuarioCancelamento = CodigoUsuarioAtual ?? UsuarioLogado?.Login ?? "Sistema";
                var sucesso = await _lancamentoService.CancelarLancamentoAsync(id, motivo, usuarioCancelamento);

                if (sucesso)
                {
                    LogarAcao("CancelarLancamento", 
                        $"Lan�amento ID: {id} cancelado. Motivo: {motivo}",
                        "LANCAMENTOS_CONSUMO", id);

                    return Json(new { sucesso = true, mensagem = "Lan�amento cancelado com sucesso." });
                }
                else
                {
                    return Json(new { sucesso = false, mensagem = "Erro ao cancelar lan�amento." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cancelar lan�amento ID: {Id}", id);
                return Json(new { sucesso = false, mensagem = "Erro interno ao cancelar lan�amento." });
            }
        }

        #endregion
    }
}