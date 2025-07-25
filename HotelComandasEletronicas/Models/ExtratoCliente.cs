using System.ComponentModel.DataAnnotations;

namespace HotelComandasEletronicas.Models
{
    /// <summary>
    /// Classe para representar o extrato completo de um cliente
    /// </summary>
    public class ExtratoCliente
    {
        public RegistroHospede Hospede { get; set; } = null!;
        public List<LancamentoConsumo> Consumos { get; set; } = new List<LancamentoConsumo>();
        public decimal TotalGasto { get; set; }
        public DateTime UltimaAtualizacao { get; set; } = DateTime.Now;
        public int TotalItens { get; set; }

        // Propriedades calculadas
        public string TotalGastoFormatado => TotalGasto.ToString("C2");
        public string UltimaAtualizacaoFormatada => UltimaAtualizacao.ToString("dd/MM/yyyy HH:mm");
        public bool TemConsumos => Consumos.Any();

        // Estatísticas por categoria
        public decimal TotalBebidas => Consumos.Where(c => c.Produto?.IsCategoria("Bebidas") == true && c.IsAtivo()).Sum(c => c.ValorTotal);
        public decimal TotalComidas => Consumos.Where(c => c.Produto?.IsCategoria("Comidas") == true && c.IsAtivo()).Sum(c => c.ValorTotal);
        public decimal TotalServicos => Consumos.Where(c => c.Produto?.IsCategoria("Serviços") == true && c.IsAtivo()).Sum(c => c.ValorTotal);

        // Métodos utilitários
        public Dictionary<string, decimal> ObterTotaisPorCategoria()
        {
            return new Dictionary<string, decimal>
            {
                ["Bebidas"] = TotalBebidas,
                ["Comidas"] = TotalComidas,
                ["Serviços"] = TotalServicos
            };
        }

        public List<LancamentoConsumo> ObterConsumosAtivos()
        {
            return Consumos.Where(c => c.IsAtivo()).OrderByDescending(c => c.DataHoraLancamento).ToList();
        }
    }
}
