using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.Services
{
    public interface IProdutoService
    {
        // Métodos de CRUD
        Task<bool> CadastrarProdutoAsync(Produto produto);
        Task<bool> AlterarProdutoAsync(Produto produto);
        Task<bool> InativarProdutoAsync(int id);
        Task<bool> AtivarProdutoAsync(int id);
        Task<bool> AlterarPrecoProdutoAsync(int id, decimal novoPreco);
        Task<Produto?> BuscarPorIdAsync(int id);

        // Métodos de busca
        Task<List<Produto>> ListarTodosAsync();
        Task<List<Produto>> ListarAtivosAsync();
        Task<List<Produto>> ListarInativosAsync();
        Task<List<Produto>> BuscarPorCategoriaAsync(string categoria);
        Task<List<Produto>> BuscarPorTextoAsync(string texto);
        Task<List<Produto>> BuscarPorFaixaPrecoAsync(decimal precoMin, decimal precoMax);

        // Métodos de validação
        Task<bool> ProdutoJaExisteAsync(string descricao);
        Task<bool> PodeInativarAsync(int id);
        Task<bool> TemLancamentosAtivosAsync(int id);

        // Métodos de categorias
        Task<List<string>> ListarCategoriasAsync();
        Task<List<Produto>> ListarBebidasAsync();
        Task<List<Produto>> ListarComidasAsync();
        Task<List<Produto>> ListarServicosAsync();

        // Métodos de relatórios
        Task<Dictionary<string, object>> ObterEstatisticasAsync();
        Task<List<Produto>> ListarMaisVendidosAsync(int quantidade = 10);
        Task<List<Produto>> ListarMenosVendidosAsync(int quantidade = 10);
        Task<decimal> CalcularValorMedioAsync();

        // Métodos utilitários
        Task<List<Produto>> BuscarComFiltrosAsync(string? descricao = null, string? categoria = null,
            bool? ativo = null, decimal? precoMin = null, decimal? precoMax = null);
        Task<int> ContarPorCategoriaAsync(string categoria);
        Task<Dictionary<string, int>> ContarPorTodasCategoriasAsync();
    }
}