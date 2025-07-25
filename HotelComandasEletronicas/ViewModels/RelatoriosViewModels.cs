using System.ComponentModel.DataAnnotations;

namespace HotelComandasEletronicas.ViewModels
{
    #region Dashboard Principal

    /// <summary>
    /// ViewModel para o dashboard principal de relatórios
    /// </summary>
    public class RelatoriosDashboardViewModel
    {
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string RelatorioSelecionado { get; set; } = string.Empty;

        // Estatísticas gerais
        public EstatisticasGeraisViewModel? EstatisticasGerais { get; set; }
        public List<VendaRecenteViewModel> VendasRecentes { get; set; } = new();
        public List<ProdutoMaisVendidoViewModel> ProdutosMaisVendidos { get; set; } = new();

        // Métodos auxiliares
        public string ObterPeriodoFormatado() => $"{DataInicio:dd/MM/yyyy} a {DataFim:dd/MM/yyyy}";
        public int DiasNoPeríodo => (DataFim - DataInicio).Days + 1;
    }

    /// <summary>
    /// ViewModel para estatísticas gerais do sistema
    /// </summary>
    public class EstatisticasGeraisViewModel
    {
        // Vendas diárias
        public decimal VendasHoje { get; set; }
        public decimal VendasOntem { get; set; }
        public int LancamentosHoje { get; set; }
        public decimal CrescimentoVendasDiario { get; set; }

        // Vendas mensais
        public decimal VendasMesAtual { get; set; }
        public decimal VendasMesPassado { get; set; }
        public decimal CrescimentoVendasMensal { get; set; }

        // Estatísticas gerais
        public int TotalHospedesAtivos { get; set; }
        public int TotalProdutosAtivos { get; set; }
        public int TotalUsuariosAtivos { get; set; }
        public decimal MediaVendaDiaria { get; set; }

        // Ticket médio
        public decimal TicketMedioHoje { get; set; }
        public decimal TicketMedioMes { get; set; }

        // Cancelamentos
        public int CancelamentosHoje { get; set; }
        public decimal TaxaCancelamentoMes { get; set; }

        // Métodos auxiliares
        public string FormatarCrescimentoDiario() => CrescimentoVendasDiario >= 0 ? $"+{CrescimentoVendasDiario:F1}%" : $"{CrescimentoVendasDiario:F1}%";
        public string FormatarCrescimentoMensal() => CrescimentoVendasMensal >= 0 ? $"+{CrescimentoVendasMensal:F1}%" : $"{CrescimentoVendasMensal:F1}%";
        public string ClasseCrescimentoDiario => CrescimentoVendasDiario >= 0 ? "text-success" : "text-danger";
        public string ClasseCrescimentoMensal => CrescimentoVendasMensal >= 0 ? "text-success" : "text-danger";
    }

    #endregion

    #region Relatórios de Vendas

    /// <summary>
    /// ViewModel para relatório de vendas
    /// </summary>
    public class RelatorioVendasViewModel
    {
        [Required]
        public DateTime DataInicio { get; set; }

        [Required]
        public DateTime DataFim { get; set; }

        public string TipoAgrupamento { get; set; } = "diario";

        public List<VendaPorPeriodoViewModel> VendasPorPeriodo { get; set; } = new();
        public ResumoVendasViewModel? ResumoVendas { get; set; }
        public List<VendaPorCategoriaViewModel> VendasPorCategoria { get; set; } = new();

        // Propriedades auxiliares
        public string ObterTituloAgrupamento() => TipoAgrupamento switch
        {
            "horario" => "Vendas por Hora",
            "semanal" => "Vendas por Semana", 
            "mensal" => "Vendas por Mês",
            _ => "Vendas por Dia"
        };
    }

    public class VendaPorPeriodoViewModel
    {
        public string Periodo { get; set; } = string.Empty;
        public decimal TotalVendas { get; set; }
        public int QuantidadeLancamentos { get; set; }
        public decimal TicketMedio { get; set; }
        public DateTime DataReferencia { get; set; }
    }

    public class ResumoVendasViewModel
    {
        public decimal TotalVendas { get; set; }
        public int QuantidadeLancamentos { get; set; }
        public decimal TicketMedio { get; set; }
        public decimal MaiorVenda { get; set; }
        public decimal MenorVenda { get; set; }
        public decimal TotalCancelamentos { get; set; }
        public int QuantidadeCancelamentos { get; set; }
        public decimal TaxaCancelamento { get; set; }
        public int DiasComVendas { get; set; }
        public decimal MediaVendasPorDia { get; set; }
    }

    public class VendaPorCategoriaViewModel
    {
        public string Categoria { get; set; } = string.Empty;
        public decimal TotalVendas { get; set; }
        public int QuantidadeLancamentos { get; set; }
        public int QuantidadeItens { get; set; }
        public decimal TicketMedio { get; set; }
        public decimal PercentualVendas { get; set; }
        public string ProdutoMaisVendido { get; set; } = string.Empty;

        public string ObterClasseBadge() => Categoria switch
        {
            "Bebidas" => "bg-info",
            "Comidas" => "bg-warning text-dark",
            "Serviços" => "bg-success",
            _ => "bg-secondary"
        };
    }

    public class VendaRecenteViewModel
    {
        public DateTime Data { get; set; }
        public decimal TotalVendas { get; set; }
        public int QuantidadeLancamentos { get; set; }
        public decimal TicketMedio { get; set; }
    }

    #endregion

    #region Relatórios de Produtos

    /// <summary>
    /// ViewModel para relatório de produtos
    /// </summary>
    public class RelatorioProdutosViewModel
    {
        [Required]
        public DateTime DataInicio { get; set; }

        [Required]
        public DateTime DataFim { get; set; }

        public string? CategoriaSelecionada { get; set; }

        public List<ProdutoMaisVendidoViewModel> ProdutosMaisVendidos { get; set; } = new();
        public List<VendaPorCategoriaViewModel> ProdutosPorCategoria { get; set; } = new();
        public List<string> CategoriasDisponiveis { get; set; } = new();
        public List<ProdutoPorCategoriaViewModel> ProdutosDaCategoria { get; set; } = new();
    }

    public class ProdutoMaisVendidoViewModel
    {
        public int ProdutoId { get; set; }
        public string NomeProduto { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public int QuantidadeVendida { get; set; }
        public decimal TotalVendas { get; set; }
        public int NumeroLancamentos { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal TicketMedio { get; set; }
        public int Ranking { get; set; }

        public string ObterClasseRanking() => Ranking switch
        {
            1 => "bg-warning text-dark",
            2 => "bg-secondary text-white",
            3 => "bg-light text-dark",
            _ => "bg-light text-muted"
        };

        public string ObterIconeRanking() => Ranking switch
        {
            1 => "fas fa-trophy",
            2 => "fas fa-medal",
            3 => "fas fa-award",
            _ => "fas fa-star"
        };
    }

    public class ProdutoPorCategoriaViewModel
    {
        public int ProdutoId { get; set; }
        public string NomeProduto { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public int QuantidadeVendida { get; set; }
        public decimal TotalVendas { get; set; }
        public decimal ValorUnitario { get; set; }
        public int NumeroLancamentos { get; set; }
    }

    public class ProdutoMaisCanceladoViewModel
    {
        public int ProdutoId { get; set; }
        public string NomeProduto { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public int QuantidadeCancelada { get; set; }
        public decimal ValorCancelado { get; set; }
        public int NumeroCancelamentos { get; set; }
    }

    #endregion

    #region Relatórios de Usuários

    /// <summary>
    /// ViewModel para relatório de usuários
    /// </summary>
    public class RelatorioUsuariosViewModel
    {
        [Required]
        public DateTime DataInicio { get; set; }

        [Required]
        public DateTime DataFim { get; set; }

        public string? UsuarioSelecionado { get; set; }

        public List<DesempenhoUsuarioViewModel> DesempenhoPorUsuario { get; set; } = new();
        public List<LancamentoPorUsuarioViewModel> LancamentosPorUsuario { get; set; } = new();
        public DetalhesUsuarioViewModel? DetalhesUsuario { get; set; }
    }

    public class DesempenhoUsuarioViewModel
    {
        public string CodigoUsuario { get; set; } = string.Empty;
        public string? NomeUsuario { get; set; }
        public decimal TotalVendas { get; set; }
        public int QuantidadeLancamentos { get; set; }
        public int QuantidadeItens { get; set; }
        public decimal TicketMedio { get; set; }
        public decimal MaiorVenda { get; set; }
        public DateTime PrimeiroPedido { get; set; }
        public DateTime UltimoPedido { get; set; }
        public int DiasTrabalhados { get; set; }
        public decimal MediaVendasPorDia { get; set; }
        public int Ranking { get; set; }

        public string ObterClasseRanking() => Ranking switch
        {
            1 => "table-warning",
            2 => "table-secondary",
            3 => "table-light",
            _ => ""
        };

        public string ObterIconeRanking() => Ranking switch
        {
            1 => "fas fa-trophy",
            2 => "fas fa-medal",
            3 => "fas fa-award",
            _ => "fas fa-star"
        };
    }

    public class LancamentoPorUsuarioViewModel
    {
        public string CodigoUsuario { get; set; } = string.Empty;
        public string? NomeUsuario { get; set; }
        public int LancamentosAtivos { get; set; }
        public int LancamentosCancelados { get; set; }
        public decimal ValorAtivo { get; set; }
        public decimal ValorCancelado { get; set; }
        public int TotalLancamentos { get; set; }
        public decimal TaxaCancelamento { get; set; }

        public string ObterClasseTaxaCancelamento() => TaxaCancelamento switch
        {
            < 5 => "text-success",
            < 15 => "text-warning",
            _ => "text-danger"
        };
    }

    public class DetalhesUsuarioViewModel
    {
        public string CodigoUsuario { get; set; } = string.Empty;
        public string? NomeUsuario { get; set; }
        public string? PerfilUsuario { get; set; }
        public DateTime DataCadastro { get; set; }
        public DateTime? UltimoAcesso { get; set; }

        // Estatísticas do período
        public decimal TotalVendas { get; set; }
        public int TotalLancamentos { get; set; }
        public int TotalCancelamentos { get; set; }
        public decimal TicketMedio { get; set; }
        public decimal TaxaCancelamento { get; set; }

        // Análise temporal
        public int DiasTrabalhados { get; set; }
        public DateTime? PrimeiroPedido { get; set; }
        public DateTime? UltimoPedido { get; set; }
        public decimal MediaVendasPorDia { get; set; }
        public decimal MediaLancamentosPorDia { get; set; }

        // Favoritos
        public string? ProdutoMaisVendido { get; set; }
        public string? CategoriaMaisVendida { get; set; }

        // Performance
        public dynamic? MelhorDia { get; set; }
        public int HorarioMaisAtivo { get; set; }

        public string FormatarHorarioMaisAtivo() => $"{HorarioMaisAtivo:00}:00h";
    }

    #endregion

    #region Relatórios de Ocupação

    /// <summary>
    /// ViewModel para relatório de ocupação
    /// </summary>
    public class RelatorioOcupacaoViewModel
    {
        [Required]
        public DateTime DataInicio { get; set; }

        [Required]
        public DateTime DataFim { get; set; }

        public List<OcupacaoPorPeriodoViewModel> OcupacaoPorPeriodo { get; set; } = new();
        public List<QuartoMaisAtivoViewModel> QuartosMaisAtivos { get; set; } = new();
        public TempoMedioEstadiaViewModel? TempoMedioEstadia { get; set; }
    }

    public class OcupacaoPorPeriodoViewModel
    {
        public DateTime Data { get; set; }
        public int NovasReservas { get; set; }
        public int ReservasAtivas { get; set; }
        public int ReservasFinalizadas { get; set; }
        public decimal ValorMedioGasto { get; set; }
        public string? QuartoMaisAtivo { get; set; }
    }

    public class QuartoMaisAtivoViewModel
    {
        public string NumeroQuarto { get; set; } = string.Empty;
        public string NomeHospede { get; set; } = string.Empty;
        public decimal TotalConsumido { get; set; }
        public int QuantidadeLancamentos { get; set; }
        public int QuantidadeItens { get; set; }
        public decimal TicketMedio { get; set; }
        public DateTime PrimeiroPedido { get; set; }
        public DateTime UltimoPedido { get; set; }
        public decimal MediaConsumoPorDia { get; set; }
        public int Ranking { get; set; }

        public int DiasAtivos => (UltimoPedido - PrimeiroPedido).Days + 1;
    }

    public class TempoMedioEstadiaViewModel
    {
        public int TotalRegistros { get; set; }
        public int RegistrosAtivos { get; set; }
        public int RegistrosFinalizados { get; set; }
        public double TempoMedioEstadia { get; set; }
        public double MenorEstadia { get; set; }
        public double MaiorEstadia { get; set; }
        public double TempoMedioAtual { get; set; }

        public string FormatarTempoMedio() => $"{TempoMedioEstadia:F1} dias";
        public string FormatarTempoAtual() => $"{TempoMedioAtual:F1} dias";
    }

    #endregion

    #region Relatórios de Cancelamentos

    /// <summary>
    /// ViewModel para relatório de cancelamentos
    /// </summary>
    public class RelatorioCancelamentosViewModel
    {
        [Required]
        public DateTime DataInicio { get; set; }

        [Required]
        public DateTime DataFim { get; set; }

        public List<CancelamentoPorPeriodoViewModel> CancelamentosPorPeriodo { get; set; } = new();
        public List<MotivoCancelamentoViewModel> MotivosCancelamento { get; set; } = new();
        public List<ProdutoMaisCanceladoViewModel> ProdutosMaisCancelados { get; set; } = new();
    }

    public class CancelamentoPorPeriodoViewModel
    {
        public DateTime Data { get; set; }
        public int QuantidadeCancelamentos { get; set; }
        public decimal ValorCancelado { get; set; }
        public decimal TicketMedioCancelado { get; set; }
        public int TotalLancamentos { get; set; }
        public decimal TaxaCancelamento { get; set; }

        public string ObterClasseTaxa() => TaxaCancelamento switch
        {
            < 5 => "text-success",
            < 15 => "text-warning",
            _ => "text-danger"
        };
    }

    public class MotivoCancelamentoViewModel
    {
        public string Motivo { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal ValorTotal { get; set; }
        public decimal Percentual { get; set; }

        public string ObterClassePercentual() => Percentual switch
        {
            > 30 => "bg-danger",
            > 15 => "bg-warning",
            > 5 => "bg-info",
            _ => "bg-light"
        };
    }

    #endregion

    #region Relatórios Personalizados

    /// <summary>
    /// ViewModel para relatório personalizado
    /// </summary>
    public class RelatorioPersonalizadoViewModel
    {
        [Required]
        public DateTime DataInicio { get; set; }

        [Required]
        public DateTime DataFim { get; set; }

        public string? TituloPersonalizado { get; set; }

        // Configurações do relatório
        public bool IncluirVendas { get; set; }
        public bool IncluirProdutos { get; set; }
        public bool IncluirUsuarios { get; set; }
        public bool IncluirOcupacao { get; set; }
        public bool IncluirCancelamentos { get; set; }

        // Filtros específicos
        public string? CategoriaFiltro { get; set; }
        public string? UsuarioFiltro { get; set; }
        public string? QuartoFiltro { get; set; }

        // Resultado
        public RelatorioPersonalizadoResultadoViewModel? Resultado { get; set; }

        public string ObterResumoConfiguracoes()
        {
            var configuracoes = new List<string>();
            if (IncluirVendas) configuracoes.Add("Vendas");
            if (IncluirProdutos) configuracoes.Add("Produtos");
            if (IncluirUsuarios) configuracoes.Add("Usuários");
            if (IncluirOcupacao) configuracoes.Add("Ocupação");
            if (IncluirCancelamentos) configuracoes.Add("Cancelamentos");

            return string.Join(", ", configuracoes);
        }
    }

    public class RelatorioPersonalizadoResultadoViewModel
    {
        public string TituloRelatorio { get; set; } = string.Empty;
        public DateTime DataGeracao { get; set; }
        public DateTime PeriodoInicio { get; set; }
        public DateTime PeriodoFim { get; set; }
        public string? ErroGeração { get; set; }

        // Dados opcionais baseados na configuração
        public ResumoVendasViewModel? ResumoVendas { get; set; }
        public List<ProdutoMaisVendidoViewModel>? ProdutosMaisVendidos { get; set; }
        public List<DesempenhoUsuarioViewModel>? DesempenhoUsuarios { get; set; }
        public List<OcupacaoPorPeriodoViewModel>? OcupacaoPorPeriodo { get; set; }
        public List<CancelamentoPorPeriodoViewModel>? CancelamentosPorPeriodo { get; set; }

        public bool TemErro => !string.IsNullOrEmpty(ErroGeração);
        public string ObterPeriodoFormatado() => $"{PeriodoInicio:dd/MM/yyyy} a {PeriodoFim:dd/MM/yyyy}";
    }

    #endregion
}