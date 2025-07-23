using System.ComponentModel.DataAnnotations;
using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.ViewModels
{
    public class ProdutoViewModel
    {
        public int ID { get; set; }

        [Required(ErrorMessage = "Descrição é obrigatória")]
        [StringLength(100, ErrorMessage = "Descrição deve ter no máximo 100 caracteres")]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "Valor é obrigatório")]
        [Range(0.01, 9999.99, ErrorMessage = "Valor deve estar entre R$ 0,01 e R$ 9.999,99")]
        public decimal Valor { get; set; }

        [Required(ErrorMessage = "Categoria é obrigatória")]
        public string Categoria { get; set; } = string.Empty;

        public bool Status { get; set; } = true;

        public DateTime DataCadastro { get; set; }

        public string UsuarioCadastro { get; set; } = string.Empty;

        // Propriedades auxiliares
        public bool IsEdicao => ID > 0;
        public string ValorFormatado => Valor.ToString("C2");

        // Lista de categorias disponíveis
        public static List<string> CategoriasDisponiveis => new List<string>
        {
            "Bebidas",
            "Comidas",
            "Serviços"
        };

        // Propriedades calculadas
        public string IconeCategoria => Categoria switch
        {
            "Bebidas" => "fas fa-glass-whiskey",
            "Comidas" => "fas fa-utensils",
            "Serviços" => "fas fa-concierge-bell",
            _ => "fas fa-box"
        };

        public string ClasseCategoria => Categoria switch
        {
            "Bebidas" => "text-info",
            "Comidas" => "text-warning",
            "Serviços" => "text-success",
            _ => "text-secondary"
        };

        public string BadgeCategoria => Categoria switch
        {
            "Bebidas" => "bg-info",
            "Comidas" => "bg-warning text-dark",
            "Serviços" => "bg-success",
            _ => "bg-secondary"
        };

        // Métodos de conversão
        public Produto ParaModel()
        {
            return new Produto
            {
                ID = this.ID,
                Descricao = this.Descricao,
                Valor = this.Valor,
                Categoria = this.Categoria,
                Status = this.Status,
                DataCadastro = this.DataCadastro,
                UsuarioCadastro = this.UsuarioCadastro
            };
        }

        public static ProdutoViewModel DeModel(Produto produto)
        {
            return new ProdutoViewModel
            {
                ID = produto.ID,
                Descricao = produto.Descricao,
                Valor = produto.Valor,
                Categoria = produto.Categoria,
                Status = produto.Status,
                DataCadastro = produto.DataCadastro,
                UsuarioCadastro = produto.UsuarioCadastro
            };
        }

        // Validações customizadas
        public bool IsValido(out List<string> erros)
        {
            erros = new List<string>();

            if (string.IsNullOrWhiteSpace(Descricao))
                erros.Add("Descrição é obrigatória");

            if (Descricao.Length > 100)
                erros.Add("Descrição deve ter no máximo 100 caracteres");

            if (Valor <= 0)
                erros.Add("Valor deve ser maior que zero");

            if (Valor > 9999.99m)
                erros.Add("Valor deve ser menor que R$ 10.000,00");

            if (string.IsNullOrWhiteSpace(Categoria))
                erros.Add("Categoria é obrigatória");

            if (!CategoriasDisponiveis.Contains(Categoria))
                erros.Add("Categoria deve ser uma das opções disponíveis");

            return !erros.Any();
        }

        // Métodos auxiliares
        public bool IsBebida() => Categoria == "Bebidas";
        public bool IsComida() => Categoria == "Comidas";
        public bool IsServico() => Categoria == "Serviços";

        public string ObterDescricaoCompleta()
        {
            return $"{Descricao} - {ValorFormatado} ({Categoria})";
        }

        public string ObterResumo()
        {
            var status = Status ? "Ativo" : "Inativo";
            return $"{Descricao} | {Categoria} | {ValorFormatado} | {status}";
        }
    }

    /// <summary>
    /// ViewModel para filtros de produtos
    /// </summary>
    public class ProdutoFiltroViewModel
    {
        public string? Descricao { get; set; }
        public string? Categoria { get; set; }
        public bool? Status { get; set; }
        public decimal? ValorMinimo { get; set; }
        public decimal? ValorMaximo { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }

        public bool TemFiltros => !string.IsNullOrWhiteSpace(Descricao) ||
                                 !string.IsNullOrWhiteSpace(Categoria) ||
                                 Status.HasValue ||
                                 ValorMinimo.HasValue ||
                                 ValorMaximo.HasValue ||
                                 DataInicio.HasValue ||
                                 DataFim.HasValue;

        public Dictionary<string, object> ParaDicionario()
        {
            var filtros = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(Descricao))
                filtros["descricao"] = Descricao;

            if (!string.IsNullOrWhiteSpace(Categoria))
                filtros["categoria"] = Categoria;

            if (Status.HasValue)
                filtros["ativo"] = Status.Value;

            if (ValorMinimo.HasValue)
                filtros["precoMin"] = ValorMinimo.Value;

            if (ValorMaximo.HasValue)
                filtros["precoMax"] = ValorMaximo.Value;

            return filtros;
        }
    }

    /// <summary>
    /// ViewModel para estatísticas de produtos
    /// </summary>
    public class ProdutoEstatisticasViewModel
    {
        public int TotalProdutos { get; set; }
        public int ProdutosAtivos { get; set; }
        public int ProdutosInativos { get; set; }
        public decimal ValorMedio { get; set; }
        public Produto? ProdutoMaisCaro { get; set; }
        public Produto? ProdutoMaisBarato { get; set; }
        public int QuantidadeBebidas { get; set; }
        public int QuantidadeComidas { get; set; }
        public int QuantidadeServicos { get; set; }

        public decimal PercentualAtivos => TotalProdutos > 0 ? (decimal)ProdutosAtivos / TotalProdutos * 100 : 0;
        public decimal PercentualInativos => TotalProdutos > 0 ? (decimal)ProdutosInativos / TotalProdutos * 100 : 0;

        public string ValorMedioFormatado => ValorMedio.ToString("C2");
        public string ProdutoMaisCaroInfo => ProdutoMaisCaro?.Descricao + " - " + ProdutoMaisCaro?.FormatarValor() ?? "Nenhum";
        public string ProdutoMaisBaratoInfo => ProdutoMaisBarato?.Descricao + " - " + ProdutoMaisBarato?.FormatarValor() ?? "Nenhum";
    }

    /// <summary>
    /// ViewModel para lançamento rápido de produtos
    /// </summary>
    public class ProdutoLancamentoViewModel
    {
        public int ID { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string IconeCategoria { get; set; } = string.Empty;
        public string ValorFormatado { get; set; } = string.Empty;

        public static ProdutoLancamentoViewModel DeModel(Produto produto)
        {
            var viewModel = new ProdutoViewModel();
            var temp = ProdutoViewModel.DeModel(produto);

            return new ProdutoLancamentoViewModel
            {
                ID = produto.ID,
                Descricao = produto.Descricao,
                Valor = produto.Valor,
                Categoria = produto.Categoria,
                IconeCategoria = temp.IconeCategoria,
                ValorFormatado = produto.FormatarValor()
            };
        }
    }
}