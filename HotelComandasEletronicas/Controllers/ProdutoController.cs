using Microsoft.AspNetCore.Mvc;
using HotelComandasEletronicas.Services;
using HotelComandasEletronicas.ViewModels;
using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.Controllers
{
    public class ProdutoController : BaseController
    {
        private readonly IProdutoService _produtoService;
        private readonly ILogger<ProdutoController> _logger;

        public ProdutoController(IProdutoService produtoService, ILogger<ProdutoController> logger)
        {
            _produtoService = produtoService;
            _logger = logger;
        }

        #region Views Principais

        /// <summary>
        /// Lista todos os produtos - Acessível para Recepção e Supervisor
        /// </summary>
        [HttpGet]
        [RequireRecepcaoOuSupervisor]
        public async Task<IActionResult> Index(string? categoria = null, string? busca = null, bool? ativo = null)
        {
            try
            {
                List<Produto> produtos;

                // Aplicar filtros se fornecidos
                if (!string.IsNullOrWhiteSpace(categoria) || !string.IsNullOrWhiteSpace(busca) || ativo.HasValue)
                {
                    produtos = await _produtoService.BuscarComFiltrosAsync(
                        descricao: busca,
                        categoria: categoria,
                        ativo: ativo
                    );
                }
                else
                {
                    produtos = await _produtoService.ListarTodosAsync();
                }

                // Dados para filtros
                ViewBag.Categorias = await _produtoService.ListarCategoriasAsync();
                ViewBag.CategoriaAtual = categoria;
                ViewBag.BuscaAtual = busca;
                ViewBag.AtivoAtual = ativo;

                // Estatísticas para o dashboard
                var estatisticas = await _produtoService.ObterEstatisticasAsync();
                ViewBag.Estatisticas = estatisticas;

                LogarAcao("ConsultarProdutos", $"Filtros: categoria={categoria}, busca={busca}, ativo={ativo}");

                return View(produtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar lista de produtos");
                DefinirMensagemErro("Erro ao carregar produtos. Tente novamente.");
                return View(new List<Produto>());
            }
        }

        /// <summary>
        /// Exibe formulário de cadastro - Apenas Supervisor
        /// </summary>
        [HttpGet]
        [RequireSupervisor]
        public async Task<IActionResult> Cadastrar()
        {
            ViewBag.Categorias = await _produtoService.ListarCategoriasAsync();
            return View(new ProdutoViewModel());
        }

        /// <summary>
        /// Processa cadastro de produto - Apenas Supervisor
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireSupervisor]
        public async Task<IActionResult> Cadastrar(ProdutoViewModel model)
        {
            ViewBag.Categorias = await _produtoService.ListarCategoriasAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verificar se produto já existe
            if (await _produtoService.ProdutoJaExisteAsync(model.Descricao))
            {
                ModelState.AddModelError("Descricao", "Já existe um produto com esta descrição.");
                return View(model);
            }

            var produto = model.ParaModel();
            produto.UsuarioCadastro = UsuarioLogado?.Login ?? "";

            var sucesso = await _produtoService.CadastrarProdutoAsync(produto);

            if (sucesso)
            {
                LogarAcao("CadastrarProduto", $"Produto: {model.Descricao} - {model.Valor:C}");
                return RedirecionarComSucesso("Produto", "Index", $"Produto '{model.Descricao}' cadastrado com sucesso!");
            }
            else
            {
                DefinirMensagemErro("Erro ao cadastrar produto. Verifique os dados e tente novamente.");
                return View(model);
            }
        }

        /// <summary>
        /// Exibe formulário de edição - Apenas Supervisor
        /// </summary>
        [HttpGet]
        [RequireSupervisor]
        public async Task<IActionResult> Editar(int id)
        {
            var produto = await _produtoService.BuscarPorIdAsync(id);
            if (produto == null)
            {
                return RedirecionarComErro("Produto", "Index", "Produto não encontrado.");
            }

            ViewBag.Categorias = await _produtoService.ListarCategoriasAsync();
            ViewBag.TemLancamentos = await _produtoService.TemLancamentosAtivosAsync(id);

            return View(ProdutoViewModel.DeModel(produto));
        }

        /// <summary>
        /// Processa edição de produto - Apenas Supervisor
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireSupervisor]
        public async Task<IActionResult> Editar(ProdutoViewModel model)
        {
            ViewBag.Categorias = await _produtoService.ListarCategoriasAsync();
            ViewBag.TemLancamentos = await _produtoService.TemLancamentosAtivosAsync(model.ID);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var produto = model.ParaModel();
            var sucesso = await _produtoService.AlterarProdutoAsync(produto);

            if (sucesso)
            {
                LogarAcao("AlterarProduto", $"Produto ID: {model.ID} - {model.Descricao}");
                return RedirecionarComSucesso("Produto", "Index", $"Produto '{model.Descricao}' alterado com sucesso!");
            }
            else
            {
                DefinirMensagemErro("Erro ao alterar produto. Verifique se a descrição não está duplicada.");
                return View(model);
            }
        }

        /// <summary>
        /// Exibe detalhes do produto
        /// </summary>
        [HttpGet]
        [RequireRecepcaoOuSupervisor]
        public async Task<IActionResult> Detalhes(int id)
        {
            var produto = await _produtoService.BuscarPorIdAsync(id);
            if (produto == null)
            {
                return RedirecionarComErro("Produto", "Index", "Produto não encontrado.");
            }

            return View(produto);
        }

        #endregion

        #region Ações AJAX

        /// <summary>
        /// Inativar produto via AJAX - Apenas Supervisor
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireSupervisor]
        public async Task<IActionResult> Inativar(int id)
        {
            // Verificar se pode inativar
            var podeInativar = await _produtoService.PodeInativarAsync(id);
            if (!podeInativar)
            {
                return Json(new { sucesso = false, mensagem = "Produto não pode ser inativado. Possui lançamentos recentes." });
            }

            var sucesso = await _produtoService.InativarProdutoAsync(id);

            if (sucesso)
            {
                LogarAcao("InativarProduto", $"Produto ID: {id}");
                return Json(new { sucesso = true, mensagem = "Produto inativado com sucesso." });
            }
            else
            {
                return Json(new { sucesso = false, mensagem = "Erro ao inativar produto." });
            }
        }

        /// <summary>
        /// Ativar produto via AJAX - Apenas Supervisor
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireSupervisor]
        public async Task<IActionResult> Ativar(int id)
        {
            var sucesso = await _produtoService.AtivarProdutoAsync(id);

            if (sucesso)
            {
                LogarAcao("AtivarProduto", $"Produto ID: {id}");
                return Json(new { sucesso = true, mensagem = "Produto ativado com sucesso." });
            }
            else
            {
                return Json(new { sucesso = false, mensagem = "Erro ao ativar produto." });
            }
        }

        /// <summary>
        /// Alterar preço via AJAX - Apenas Supervisor
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireSupervisor]
        public async Task<IActionResult> AlterarPreco([FromBody] AlterarPrecoRequest request)
        {
            if (request.NovoPreco <= 0)
            {
                return Json(new { sucesso = false, mensagem = "Preço deve ser maior que zero." });
            }

            var sucesso = await _produtoService.AlterarPrecoProdutoAsync(request.ProdutoId, request.NovoPreco);

            if (sucesso)
            {
                LogarAcao("AlterarPreco", $"Produto ID: {request.ProdutoId} - Novo preço: {request.NovoPreco:C}");
                return Json(new { sucesso = true, mensagem = $"Preço alterado para {request.NovoPreco:C}." });
            }
            else
            {
                return Json(new { sucesso = false, mensagem = "Erro ao alterar preço." });
            }
        }

        /// <summary>
        /// Buscar produtos por texto (para autocomplete)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> BuscarPorTexto(string termo)
        {
            if (string.IsNullOrWhiteSpace(termo) || termo.Length < 2)
            {
                return Json(new List<object>());
            }

            var produtos = await _produtoService.BuscarPorTextoAsync(termo);

            var resultado = produtos.Select(p => new
            {
                id = p.ID,
                descricao = p.Descricao,
                valor = p.Valor,
                categoria = p.Categoria,
                valorFormatado = p.FormatarValor()
            }).ToList();

            return Json(resultado);
        }

        /// <summary>
        /// Listar produtos por categoria (para seleção rápida)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ListarPorCategoria(string categoria)
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

        /// <summary>
        /// Verificar se produto já existe
        /// </summary>
        [HttpGet]
        [RequireSupervisor]
        public async Task<IActionResult> VerificarProduto(string descricao, int? id = null)
        {
            if (string.IsNullOrWhiteSpace(descricao))
            {
                return Json(new { existe = false });
            }

            var existe = await _produtoService.ProdutoJaExisteAsync(descricao);

            // Se está editando, verificar se não é o mesmo produto
            if (existe && id.HasValue)
            {
                var produto = await _produtoService.BuscarPorIdAsync(id.Value);
                existe = produto?.Descricao != descricao;
            }

            return Json(new { existe = existe });
        }

        /// <summary>
        /// Obter estatísticas dos produtos
        /// </summary>
        [HttpGet]
        [RequireRecepcaoOuSupervisor]
        public async Task<IActionResult> ObterEstatisticas()
        {
            var estatisticas = await _produtoService.ObterEstatisticasAsync();
            return Json(estatisticas);
        }

        #endregion

        #region Views Públicas (para lançamento)

        /// <summary>
        /// Lista produtos ativos por categoria - Público (para lançamento)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ListarAtivos(string? categoria = null)
        {
            List<Produto> produtos;

            if (!string.IsNullOrWhiteSpace(categoria))
            {
                produtos = await _produtoService.BuscarPorCategoriaAsync(categoria);
            }
            else
            {
                produtos = await _produtoService.ListarAtivosAsync();
            }

            ViewBag.Categorias = await _produtoService.ListarCategoriasAsync();
            ViewBag.CategoriaAtual = categoria;

            return View(produtos);
        }

        #endregion
    }

    #region DTOs para Requests

    /// <summary>
    /// DTO para alterar preço de produto
    /// </summary>
    public class AlterarPrecoRequest
    {
        public int ProdutoId { get; set; }
        public decimal NovoPreco { get; set; }
    }

    #endregion
}