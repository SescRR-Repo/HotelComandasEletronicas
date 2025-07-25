using HotelComandasEletronicas.ViewModels;

namespace HotelComandasEletronicas.Services
{
    /// <summary>
    /// Interface para serviços de relatórios do sistema
    /// </summary>
    public interface IRelatorioService
    {
        #region Estatísticas Gerais
        Task<EstatisticasGeraisViewModel> ObterEstatisticasGeraisAsync();
        Task<List<VendaRecenteViewModel>> ObterVendasRecentesAsync(int dias);
        #endregion

        #region Relatórios de Vendas
        Task<List<VendaPorPeriodoViewModel>> ObterVendasPorPeriodoAsync(DateTime inicio, DateTime fim, string agrupamento);
        Task<ResumoVendasViewModel> ObterResumoVendasAsync(DateTime inicio, DateTime fim);
        Task<List<VendaPorCategoriaViewModel>> ObterVendasPorCategoriaAsync(DateTime inicio, DateTime fim);
        #endregion

        #region Relatórios de Produtos
        Task<List<ProdutoMaisVendidoViewModel>> ObterProdutosMaisVendidosAsync(DateTime inicio, DateTime fim, int top = 10);
        Task<List<ProdutoPorCategoriaViewModel>> ObterProdutosPorCategoriaAsync(string categoria, DateTime inicio, DateTime fim);
        Task<List<string>> ObterCategoriasAsync();
        Task<List<ProdutoMaisCanceladoViewModel>> ObterProdutosMaisCanceladosAsync(DateTime inicio, DateTime fim, int top = 10);
        #endregion

        #region Relatórios de Usuários
        Task<List<DesempenhoUsuarioViewModel>> ObterDesempenhoPorUsuarioAsync(DateTime inicio, DateTime fim);
        Task<List<LancamentoPorUsuarioViewModel>> ObterLancamentosPorUsuarioAsync(DateTime inicio, DateTime fim);
        Task<DetalhesUsuarioViewModel> ObterDetalhesUsuarioAsync(string codigoUsuario, DateTime inicio, DateTime fim);
        #endregion

        #region Relatórios de Ocupação
        Task<List<OcupacaoPorPeriodoViewModel>> ObterOcupacaoPorPeriodoAsync(DateTime inicio, DateTime fim);
        Task<List<QuartoMaisAtivoViewModel>> ObterQuartosMaisAtivosAsync(DateTime inicio, DateTime fim, int top = 10);
        Task<TempoMedioEstadiaViewModel> ObterTempoMedioEstadiaAsync(DateTime inicio, DateTime fim);
        #endregion

        #region Relatórios de Cancelamentos
        Task<List<CancelamentoPorPeriodoViewModel>> ObterCancelamentosPorPeriodoAsync(DateTime inicio, DateTime fim);
        Task<List<MotivoCancelamentoViewModel>> ObterMotivosCancelamentoAsync(DateTime inicio, DateTime fim);
        #endregion

        #region Dados para Gráficos
        Task<object> ObterDadosGraficoVendasAsync(DateTime inicio, DateTime fim, string agrupamento);
        Task<object> ObterDadosGraficoCategoriasAsync(DateTime inicio, DateTime fim);
        Task<object> ObterDadosGraficoUsuariosAsync(DateTime inicio, DateTime fim);
        #endregion

        #region Exportação
        Task<byte[]> ExportarParaExcelAsync(string tipoRelatorio, DateTime inicio, DateTime fim, string? filtros = null);
        Task<byte[]> ExportarParaPdfAsync(string tipoRelatorio, DateTime inicio, DateTime fim, string? filtros = null);
        #endregion

        #region Relatórios Personalizados
        Task<RelatorioPersonalizadoResultadoViewModel> GerarRelatorioPersonalizadoAsync(RelatorioPersonalizadoViewModel configuracao);
        #endregion
    }
}