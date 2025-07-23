using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.Services
{
    public interface IRelatorioService
    {
        // Relatórios de extrato
        Task<ExtratoCompleto> GerarExtratoAsync(int hospedeId);
        Task<RelatorioDiario> GerarRelatorioDiarioAsync(DateTime data);
        Task<RelatorioGerencial> GerarRelatorioGerencialAsync(DateTime inicio, DateTime fim);

        // Relatórios de performance
        Task<List<Produto>> GetTopProdutosAsync(DateTime inicio, DateTime fim, int quantidade = 10);
        Task<List<object>> GetRankingUsuariosAsync(DateTime inicio, DateTime fim);
        Task<Dictionary<string, decimal>> GetVendasPorDiaAsync(DateTime inicio, DateTime fim);

        // Métodos de exportação
        Task<bool> ExportarExcelAsync(object dados, string arquivo);
        Task<byte[]> GerarPdfAsync(object relatorio);

        // Métodos de estatísticas
        Task<decimal> GetTotalVendasPeriodoAsync(DateTime inicio, DateTime fim);
        Task<Dictionary<string, object>> GetKPIsDashboardAsync();
    }

    public class ExtratoCompleto
    {
        public RegistroHospede Hospede { get; set; } = null!;
        public List<LancamentoConsumo> Lancamentos { get; set; } = new();
        public decimal TotalGeral { get; set; }
        public DateTime DataGeracao { get; set; }
    }

    public class RelatorioDiario
    {
        public DateTime Data { get; set; }
        public decimal TotalVendas { get; set; }
        public int TotalLancamentos { get; set; }
        public List<Produto> ProdutosMaisVendidos { get; set; } = new();
        public Dictionary<string, int> LancamentosPorHora { get; set; } = new();
    }

    public class RelatorioGerencial
    {
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public decimal TotalVendas { get; set; }
        public int TotalLancamentos { get; set; }
        public decimal TicketMedio { get; set; }
        public List<Produto> TopProdutos { get; set; } = new();
        public List<object> RankingUsuarios { get; set; } = new();
        public Dictionary<string, decimal> VendasPorDia { get; set; } = new();
    }
}