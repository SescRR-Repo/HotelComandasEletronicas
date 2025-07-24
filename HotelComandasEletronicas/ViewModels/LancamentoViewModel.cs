using System.ComponentModel.DataAnnotations;
using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.ViewModels
{
    /// <summary>
    /// ViewModel para lançamento de consumo
    /// </summary>
    public class LancamentoViewModel
    {
        [Required(ErrorMessage = "Selecione um hóspede")]
        public int RegistroHospedeID { get; set; }

        [Required(ErrorMessage = "Selecione um produto")]
        public int ProdutoID { get; set; }

        [Required(ErrorMessage = "Quantidade é obrigatória")]
        [Range(0.01, 999.99, ErrorMessage = "Quantidade deve estar entre 0,01 e 999,99")]
        public decimal Quantidade { get; set; } = 1;

        [StringLength(200, ErrorMessage = "Observações deve ter no máximo 200 caracteres")]
        public string? Observacoes { get; set; }

        // Propriedades auxiliares para a interface
        public string? NumeroQuarto { get; set; }
        public string? NomeCliente { get; set; }
        public string? DescricaoProduto { get; set; }
        public string? CategoriaProduto { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal ValorTotal => Quantidade * ValorUnitario;

        // Métodos de validação
        public bool IsValido()
        {
            return RegistroHospedeID > 0 &&
                   ProdutoID > 0 &&
                   Quantidade > 0 &&
                   Quantidade <= 999.99m;
        }

        public void LimparDados()
        {
            RegistroHospedeID = 0;
            ProdutoID = 0;
            Quantidade = 1;
            Observacoes = string.Empty;
            NumeroQuarto = string.Empty;
            NomeCliente = string.Empty;
            DescricaoProduto = string.Empty;
            CategoriaProduto = string.Empty;
            ValorUnitario = 0;
        }

        // Métodos auxiliares
        public string ObterResumoLancamento()
        {
            return $"{DescricaoProduto} x{Quantidade} = {ValorTotal:C2}";
        }

        public string ObterInformacaoCompleta()
        {
            return $"Quarto {NumeroQuarto} - {NomeCliente}: {ObterResumoLancamento()}";
        }

        // Conversão para model
        public LancamentoConsumo ParaModel(string codigoUsuario)
        {
            return new LancamentoConsumo
            {
                RegistroHospedeID = this.RegistroHospedeID,
                ProdutoID = this.ProdutoID,
                Quantidade = this.Quantidade,
                ValorUnitario = this.ValorUnitario,
                ValorTotal = this.ValorTotal,
                DataHoraLancamento = DateTime.Now,
                CodigoUsuarioLancamento = codigoUsuario,
                Status = "Ativo"
            };
        }
    }

    /// <summary>
    /// ViewModel para histórico de lançamentos
    /// </summary>
    public class HistoricoLancamentoViewModel
    {
        public DateTime DataInicio { get; set; } = DateTime.Today.AddDays(-7);
        public DateTime DataFim { get; set; } = DateTime.Today.AddDays(1);
        public string? UsuarioFiltro { get; set; }
        public string? QuartoFiltro { get; set; }
        public string? StatusFiltro { get; set; }

        public List<LancamentoConsumo> Lancamentos { get; set; } = new();

        // Propriedades calculadas
        public decimal ValorTotalPeriodo => Lancamentos.Where(l => l.IsAtivo()).Sum(l => l.ValorTotal);
        public int TotalLancamentos => Lancamentos.Count;
        public int LancamentosAtivos => Lancamentos.Count(l => l.IsAtivo());
        public int LancamentosCancelados => Lancamentos.Count(l => !l.IsAtivo());

        // Métodos auxiliares
        public Dictionary<string, int> ObterResumoStatus()
        {
            return new Dictionary<string, int>
            {
                ["Ativos"] = LancamentosAtivos,
                ["Cancelados"] = LancamentosCancelados,
                ["Total"] = TotalLancamentos
            };
        }

        public Dictionary<string, decimal> ObterResumoPorCategoria()
        {
            return Lancamentos
                .Where(l => l.Status == "Ativo" && l.Produto != null)
                .GroupBy(l => l.Produto!.Categoria)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.ValorTotal));
        }

        public List<LancamentoConsumo> ObterLancamentosRecentes(int quantidade = 10)
        {
            return Lancamentos
                .OrderByDescending(l => l.DataHoraLancamento)
                .Take(quantidade)
                .ToList();
        }
    }

    /// <summary>
    /// ViewModel para cancelamento de lançamento
    /// </summary>
    public class CancelamentoLancamentoViewModel
    {
        [Required(ErrorMessage = "ID do lançamento é obrigatório")]
        public int LancamentoID { get; set; }

        [Required(ErrorMessage = "Motivo do cancelamento é obrigatório")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Motivo deve ter entre 5 e 200 caracteres")]
        public string Motivo { get; set; } = string.Empty;

        public string? UsuarioCancelamento { get; set; }

        // Propriedades informativas do lançamento
        public string? DescricaoProduto { get; set; }
        public string? NumeroQuarto { get; set; }
        public string? NomeCliente { get; set; }
        public decimal ValorLancamento { get; set; }
        public DateTime DataLancamento { get; set; }

        // Métodos de validação
        public bool IsValido()
        {
            return LancamentoID > 0 &&
                   !string.IsNullOrWhiteSpace(Motivo) &&
                   Motivo.Length >= 5 &&
                   Motivo.Length <= 200;
        }

        public string ObterResumoDetalhado()
        {
            return $"Quarto {NumeroQuarto} - {NomeCliente}: {DescricaoProduto} ({ValorLancamento:C2}) - {DataLancamento:dd/MM/yyyy HH:mm}";
        }
    }

    /// <summary>
    /// ViewModel para carrinho de compras (lançamentos múltiplos)
    /// </summary>
    public class CarrinhoLancamentoViewModel
    {
        public int RegistroHospedeID { get; set; }
        public string? NumeroQuarto { get; set; }
        public string? NomeCliente { get; set; }

        public List<ItemCarrinhoViewModel> Itens { get; set; } = new();

        // Propriedades calculadas
        public decimal ValorTotalCarrinho => Itens.Sum(i => i.ValorTotal);
        public int TotalItens => Itens.Count;

        // Métodos
        public void AdicionarItem(int produtoId, string descricao, decimal quantidade, decimal valorUnitario, string? observacoes = null)
        {
            var item = new ItemCarrinhoViewModel
            {
                ProdutoID = produtoId,
                DescricaoProduto = descricao,
                Quantidade = quantidade,
                ValorUnitario = valorUnitario,
                Observacoes = observacoes
            };

            Itens.Add(item);
        }

        public void RemoverItem(int index)
        {
            if (index >= 0 && index < Itens.Count)
            {
                Itens.RemoveAt(index);
            }
        }

        public void LimparCarrinho()
        {
            Itens.Clear();
        }

        public bool TemItens()
        {
            return Itens.Any();
        }
    }

    /// <summary>
    /// ViewModel para item do carrinho
    /// </summary>
    public class ItemCarrinhoViewModel
    {
        public int ProdutoID { get; set; }
        public string DescricaoProduto { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal ValorTotal => Quantidade * ValorUnitario;
        public string? Observacoes { get; set; }

        public string ObterResumo()
        {
            return $"{DescricaoProduto} x{Quantidade} = {ValorTotal:C2}";
        }
    }
}