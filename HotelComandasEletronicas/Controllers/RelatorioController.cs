using Microsoft.AspNetCore.Mvc;
using HotelComandasEletronicas.Services;
using HotelComandasEletronicas.ViewModels;

namespace HotelComandasEletronicas.Controllers
{
    /// <summary>
    /// Controller para relatórios gerenciais do sistema
    /// Acesso restrito a Recepção e Supervisor
    /// </summary>
    [RequireRecepcaoOuSupervisor]
    public class RelatorioController : BaseController
    {
        private readonly IRelatorioService _relatorioService;
        private readonly ILogger<RelatorioController> _logger;

        public RelatorioController(IRelatorioService relatorioService, ILogger<RelatorioController> logger)
        {
            _relatorioService = relatorioService;
            _logger = logger;
        }

        #region Views Principais

        /// <summary>
        /// Dashboard principal de relatórios
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var viewModel = new RelatoriosDashboardViewModel
                {
                    DataInicio = DateTime.Today.AddDays(-30),
                    DataFim = DateTime.Today,
                    RelatorioSelecionado = "vendas"
                };

                // Carregar estatísticas gerais
                viewModel.EstatisticasGerais = await _relatorioService.ObterEstatisticasGeraisAsync();
                viewModel.VendasRecentes = await _relatorioService.ObterVendasRecentesAsync(7);
                viewModel.ProdutosMaisVendidos = await _relatorioService.ObterProdutosMaisVendidosAsync(DateTime.Today.AddDays(-30), DateTime.Today, 5);

                LogarAcao("AcessoRelatorios", "Dashboard de relatórios acessado");

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar dashboard de relatórios");
                DefinirMensagemErro("Erro ao carregar relatórios. Tente novamente.");
                return RedirecionarParaHome();
            }
        }

        /// <summary>
        /// Relatório de vendas por período
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Vendas(DateTime? inicio = null, DateTime? fim = null, string? agrupamento = null)
        {
            try
            {
                inicio ??= DateTime.Today.AddDays(-30);
                fim ??= DateTime.Today;
                agrupamento ??= "diario";

                var viewModel = new RelatorioVendasViewModel
                {
                    DataInicio = inicio.Value,
                    DataFim = fim.Value,
                    TipoAgrupamento = agrupamento
                };

                // Obter dados do relatório
                viewModel.VendasPorPeriodo = await _relatorioService.ObterVendasPorPeriodoAsync(inicio.Value, fim.Value, agrupamento);
                viewModel.ResumoVendas = await _relatorioService.ObterResumoVendasAsync(inicio.Value, fim.Value);
                viewModel.VendasPorCategoria = await _relatorioService.ObterVendasPorCategoriaAsync(inicio.Value, fim.Value);

                LogarAcao("RelatorioVendas", $"Período: {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}");

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar relatório de vendas");
                DefinirMensagemErro("Erro ao gerar relatório de vendas.");
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Relatório de produtos mais vendidos
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Produtos(DateTime? inicio = null, DateTime? fim = null, string? categoria = null)
        {
            try
            {
                inicio ??= DateTime.Today.AddDays(-30);
                fim ??= DateTime.Today;

                var viewModel = new RelatorioProdutosViewModel
                {
                    DataInicio = inicio.Value,
                    DataFim = fim.Value,
                    CategoriaSelecionada = categoria
                };

                // Obter dados do relatório
                viewModel.ProdutosMaisVendidos = await _relatorioService.ObterProdutosMaisVendidosAsync(inicio.Value, fim.Value, 20);
                viewModel.ProdutosPorCategoria = await _relatorioService.ObterVendasPorCategoriaAsync(inicio.Value, fim.Value);
                viewModel.CategoriasDisponiveis = await _relatorioService.ObterCategoriasAsync();

                if (!string.IsNullOrWhiteSpace(categoria))
                {
                    viewModel.ProdutosDaCategoria = await _relatorioService.ObterProdutosPorCategoriaAsync(categoria, inicio.Value, fim.Value);
                }

                LogarAcao("RelatorioProdutos", $"Período: {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}, Categoria: {categoria ?? "Todas"}");

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar relatório de produtos");
                DefinirMensagemErro("Erro ao gerar relatório de produtos.");
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Relatório de desempenho por usuário/garçom
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Usuarios(DateTime? inicio = null, DateTime? fim = null, string? usuario = null)
        {
            try
            {
                inicio ??= DateTime.Today.AddDays(-30);
                fim ??= DateTime.Today;

                var viewModel = new RelatorioUsuariosViewModel
                {
                    DataInicio = inicio.Value,
                    DataFim = fim.Value,
                    UsuarioSelecionado = usuario
                };

                // Obter dados do relatório
                viewModel.DesempenhoPorUsuario = await _relatorioService.ObterDesempenhoPorUsuarioAsync(inicio.Value, fim.Value);
                viewModel.LancamentosPorUsuario = await _relatorioService.ObterLancamentosPorUsuarioAsync(inicio.Value, fim.Value);

                if (!string.IsNullOrWhiteSpace(usuario))
                {
                    viewModel.DetalhesUsuario = await _relatorioService.ObterDetalhesUsuarioAsync(usuario, inicio.Value, fim.Value);
                }

                LogarAcao("RelatorioUsuarios", $"Período: {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}, Usuário: {usuario ?? "Todos"}");

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar relatório de usuários");
                DefinirMensagemErro("Erro ao gerar relatório de usuários.");
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Relatório de ocupação de quartos
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Ocupacao(DateTime? inicio = null, DateTime? fim = null)
        {
            try
            {
                inicio ??= DateTime.Today.AddDays(-30);
                fim ??= DateTime.Today;

                var viewModel = new RelatorioOcupacaoViewModel
                {
                    DataInicio = inicio.Value,
                    DataFim = fim.Value
                };

                // Obter dados do relatório
                viewModel.OcupacaoPorPeriodo = await _relatorioService.ObterOcupacaoPorPeriodoAsync(inicio.Value, fim.Value);
                viewModel.QuartosMaisAtivos = await _relatorioService.ObterQuartosMaisAtivosAsync(inicio.Value, fim.Value, 10);
                viewModel.TempoMedioEstadia = await _relatorioService.ObterTempoMedioEstadiaAsync(inicio.Value, fim.Value);

                LogarAcao("RelatorioOcupacao", $"Período: {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}");

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar relatório de ocupação");
                DefinirMensagemErro("Erro ao gerar relatório de ocupação.");
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Relatório de cancelamentos
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Cancelamentos(DateTime? inicio = null, DateTime? fim = null)
        {
            try
            {
                inicio ??= DateTime.Today.AddDays(-30);
                fim ??= DateTime.Today;

                var viewModel = new RelatorioCancelamentosViewModel
                {
                    DataInicio = inicio.Value,
                    DataFim = fim.Value
                };

                // Obter dados do relatório
                viewModel.CancelamentosPorPeriodo = await _relatorioService.ObterCancelamentosPorPeriodoAsync(inicio.Value, fim.Value);
                viewModel.MotivosCancelamento = await _relatorioService.ObterMotivosCancelamentoAsync(inicio.Value, fim.Value);
                viewModel.ProdutosMaisCancelados = await _relatorioService.ObterProdutosMaisCanceladosAsync(inicio.Value, fim.Value, 10);

                LogarAcao("RelatorioCancelamentos", $"Período: {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}");

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar relatório de cancelamentos");
                DefinirMensagemErro("Erro ao gerar relatório de cancelamentos.");
                return RedirectToAction("Index");
            }
        }

        #endregion

        #region APIs para Gráficos (AJAX)

        /// <summary>
        /// Dados para gráfico de vendas por período
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DadosGraficoVendas(DateTime inicio, DateTime fim, string agrupamento = "diario")
        {
            try
            {
                var dados = await _relatorioService.ObterDadosGraficoVendasAsync(inicio, fim, agrupamento);
                return Json(dados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter dados para gráfico de vendas");
                return Json(new { erro = "Erro ao carregar dados do gráfico" });
            }
        }

        /// <summary>
        /// Dados para gráfico de produtos por categoria
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DadosGraficoCategorias(DateTime inicio, DateTime fim)
        {
            try
            {
                var dados = await _relatorioService.ObterDadosGraficoCategoriasAsync(inicio, fim);
                return Json(dados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter dados para gráfico de categorias");
                return Json(new { erro = "Erro ao carregar dados do gráfico" });
            }
        }

        /// <summary>
        /// Dados para gráfico de desempenho por usuário
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DadosGraficoUsuarios(DateTime inicio, DateTime fim)
        {
            try
            {
                var dados = await _relatorioService.ObterDadosGraficoUsuariosAsync(inicio, fim);
                return Json(dados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter dados para gráfico de usuários");
                return Json(new { erro = "Erro ao carregar dados do gráfico" });
            }
        }

        #endregion

        #region Exportação de Relatórios

        /// <summary>
        /// Exportar relatório para Excel
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportarExcel(string tipoRelatorio, DateTime inicio, DateTime fim, string? filtros = null)
        {
            try
            {
                var arquivoBytes = await _relatorioService.ExportarParaExcelAsync(tipoRelatorio, inicio, fim, filtros);
                var nomeArquivo = $"Relatorio_{tipoRelatorio}_{inicio:yyyyMMdd}_{fim:yyyyMMdd}.xlsx";

                LogarAcao("ExportarRelatorio", $"Tipo: {tipoRelatorio}, Período: {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}");

                return File(arquivoBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nomeArquivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao exportar relatório para Excel");
                DefinirMensagemErro("Erro ao exportar relatório. Tente novamente.");
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Exportar relatório para PDF
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportarPdf(string tipoRelatorio, DateTime inicio, DateTime fim, string? filtros = null)
        {
            try
            {
                var arquivoBytes = await _relatorioService.ExportarParaPdfAsync(tipoRelatorio, inicio, fim, filtros);
                var nomeArquivo = $"Relatorio_{tipoRelatorio}_{inicio:yyyyMMdd}_{fim:yyyyMMdd}.pdf";

                LogarAcao("ExportarRelatorio", $"Tipo: {tipoRelatorio}, Período: {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy} (PDF)");

                return File(arquivoBytes, "application/pdf", nomeArquivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao exportar relatório para PDF");
                DefinirMensagemErro("Erro ao exportar relatório. Tente novamente.");
                return RedirectToAction("Index");
            }
        }

        #endregion

        #region Relatórios Personalizados

        /// <summary>
        /// Criar relatório personalizado
        /// </summary>
        [HttpGet]
        [RequireSupervisor]
        public IActionResult Personalizado()
        {
            var viewModel = new RelatorioPersonalizadoViewModel
            {
                DataInicio = DateTime.Today.AddDays(-30),
                DataFim = DateTime.Today
            };

            return View(viewModel);
        }

        /// <summary>
        /// Processar relatório personalizado
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireSupervisor]
        public async Task<IActionResult> Personalizado(RelatorioPersonalizadoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                model.Resultado = await _relatorioService.GerarRelatorioPersonalizadoAsync(model);

                LogarAcao("RelatorioPersonalizado", $"Configuração: {model.ObterResumoConfiguracoes()}");

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar relatório personalizado");
                DefinirMensagemErro("Erro ao gerar relatório personalizado.");
                return View(model);
            }
        }

        #endregion

        #region Métodos Auxiliares

        /// <summary>
        /// Atualizar dados em tempo real (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AtualizarEstatisticas()
        {
            try
            {
                var estatisticas = await _relatorioService.ObterEstatisticasGeraisAsync();
                return Json(estatisticas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar estatísticas");
                return Json(new { erro = "Erro ao atualizar dados" });
            }
        }

        /// <summary>
        /// Obter períodos pré-definidos
        /// </summary>
        [HttpGet]
        public IActionResult ObterPeriodos()
        {
            var periodos = new
            {
                hoje = new { inicio = DateTime.Today, fim = DateTime.Today },
                ontem = new { inicio = DateTime.Today.AddDays(-1), fim = DateTime.Today.AddDays(-1) },
                semanaAtual = new { inicio = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek), fim = DateTime.Today },
                mesAtual = new { inicio = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), fim = DateTime.Today },
                ultimos7dias = new { inicio = DateTime.Today.AddDays(-7), fim = DateTime.Today },
                ultimos30dias = new { inicio = DateTime.Today.AddDays(-30), fim = DateTime.Today },
                mesPassado = new { inicio = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1), fim = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddDays(-1) }
            };

            return Json(periodos);
        }

        #endregion
    }
}
