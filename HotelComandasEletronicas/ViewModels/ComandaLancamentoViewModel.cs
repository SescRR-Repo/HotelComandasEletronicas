using System.ComponentModel.DataAnnotations;

namespace HotelComandasEletronicas.ViewModels
{
    /// <summary>
    /// ViewModel para sistema de comanda eletrônica com pré-lançamentos
    /// </summary>
    public class ComandaLancamentoViewModel
    {
        public int? RegistroHospedeID { get; set; }
        public string? NumeroQuarto { get; set; }
        public string? NomeCliente { get; set; }
        public string? TelefoneCliente { get; set; }
        public decimal ValorGastoAtual { get; set; }

        public List<ItemPreLancamento> ItensPedido { get; set; } = new();

        // Propriedades calculadas
        public decimal ValorTotalComanda => ItensPedido.Sum(i => i.ValorTotal);
        public int TotalItens => ItensPedido.Count;
        public int TotalQuantidade => (int)ItensPedido.Sum(i => i.Quantidade);

        // Métodos auxiliares
        public bool TemItensPendentes() => ItensPedido.Any();
        public bool ClienteSelecionado() => RegistroHospedeID.HasValue;

        public string ObterResumoComanda()
        {
            if (!TemItensPendentes()) return "Comanda vazia";
            return $"{TotalItens} pedido(s) - {ValorTotalComanda:C2}";
        }
    }

    /// <summary>
    /// Item individual do pré-lançamento (pedido em comanda)
    /// </summary>
    public class ItemPreLancamento
    {
        public int ProdutoID { get; set; }
        public string DescricaoProduto { get; set; } = string.Empty;
        public string CategoriaProduto { get; set; } = string.Empty;
        public decimal Quantidade { get; set; } = 1;
        public decimal ValorUnitario { get; set; }
        public decimal ValorTotal => Quantidade * ValorUnitario;
        public string? ObservacoesPedido { get; set; }

        // Métodos auxiliares
        public string ObterResumo()
        {
            var qtdFormatada = Quantidade % 1 == 0 ? Quantidade.ToString("0") : Quantidade.ToString("0.00");
            return $"{qtdFormatada}x {DescricaoProduto} = {ValorTotal:C2}";
        }

        public string ValorUnitarioFormatado => ValorUnitario.ToString("C2");
        public string ValorTotalFormatado => ValorTotal.ToString("C2");
    }

    /// <summary>
    /// ViewModel para processar a comanda completa
    /// </summary>
    public class ProcessarComandaViewModel
    {
        [Required]
        public int RegistroHospedeID { get; set; }

        [Required]
        [StringLength(2, MinimumLength = 2)]
        [RegularExpression(@"^\d{2}$", ErrorMessage = "Código do garçom deve ter 2 dígitos")]
        public string CodigoGarcom { get; set; } = string.Empty;

        public List<ItemPedidoProcessamento> ItensPedido { get; set; } = new();

        public string? ObservacoesComanda { get; set; }
    }

    /// <summary>
    /// Item para processamento da comanda
    /// </summary>
    public class ItemPedidoProcessamento
    {
        public int ProdutoID { get; set; }
        public decimal Quantidade { get; set; }
        public string? ObservacoesPedido { get; set; }
    }
}
