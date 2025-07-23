using Microsoft.EntityFrameworkCore;
using HotelComandasEletronicas.Data;
using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.Services
{
    public class ProdutoService : IProdutoService
    {
        private readonly ComandasDbContext _context;
        private readonly ILogger<ProdutoService> _logger;

        public ProdutoService(ComandasDbContext context, ILogger<ProdutoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Métodos de CRUD

        public async Task<bool> CadastrarProdutoAsync(Produto produto)
        {
            try
            {
                // Validações
                if (await ProdutoJaExisteAsync(produto.Descricao))
                {
                    _logger.LogWarning("Tentativa de cadastrar produto já existente: {Descricao}", produto.Descricao);
                    return false;
                }

                if (produto.Valor <= 0)
                {
                    _logger.LogWarning("Tentativa de cadastrar produto com valor inválido: {Valor}", produto.Valor);
                    return false;
                }

                produto.DataCadastro = DateTime.Now;
                produto.Status = true;

                _context.Produtos.Add(produto);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Produto cadastrado com sucesso: {Descricao} - {Valor:C}",
                    produto.Descricao, produto.Valor);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cadastrar produto: {Descricao}", produto.Descricao);
                return false;
            }
        }

        public async Task<bool> AlterarProdutoAsync(Produto produto)
        {
            try
            {
                var produtoExistente = await _context.Produtos.FindAsync(produto.ID);
                if (produtoExistente == null)
                    return false;

                // Verificar se a nova descrição já existe (exceto o próprio produto)
                if (produto.Descricao != produtoExistente.Descricao)
                {
                    var descricaoExiste = await _context.Produtos
                        .AnyAsync(p => p.Descricao == produto.Descricao && p.ID != produto.ID);
                    if (descricaoExiste)
                        return false;
                }

                if (produto.Valor <= 0)
                    return false;

                // Atualizar campos
                produtoExistente.Descricao = produto.Descricao;
                produtoExistente.Valor = produto.Valor;
                produtoExistente.Categoria = produto.Categoria;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Produto alterado com sucesso: {Descricao} (ID: {ID})", produto.Descricao, produto.ID);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao alterar produto: {ID}", produto.ID);
                return false;
            }
        }

        public async Task<bool> InativarProdutoAsync(int id)
        {
            try
            {
                var produto = await _context.Produtos.FindAsync(id);
                if (produto == null)
                    return false;

                produto.Inativar();
                await _context.SaveChangesAsync();

                _logger.LogInformation("Produto inativado: {Descricao} (ID: {ID})", produto.Descricao, id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inativar produto: {ID}", id);
                return false;
            }
        }

        public async Task<bool> AtivarProdutoAsync(int id)
        {
            try
            {
                var produto = await _context.Produtos.FindAsync(id);
                if (produto == null)
                    return false;

                produto.Ativar();
                await _context.SaveChangesAsync();

                _logger.LogInformation("Produto ativado: {Descricao} (ID: {ID})", produto.Descricao, id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao ativar produto: {ID}", id);
                return false;
            }
        }

        public async Task<bool> AlterarPrecoProdutoAsync(int id, decimal novoPreco)
        {
            try
            {
                var produto = await _context.Produtos.FindAsync(id);
                if (produto == null || novoPreco <= 0)
                    return false;

                var precoAnterior = produto.Valor;
                produto.AlterarPreco(novoPreco);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Preço alterado para produto {Descricao}: {PrecoAnterior:C} → {NovoPreco:C}",
                    produto.Descricao, precoAnterior, novoPreco);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao alterar preço do produto: {ID}", id);
                return false;
            }
        }

        public async Task<Produto?> BuscarPorIdAsync(int id)
        {
            try
            {
                return await _context.Produtos
                    .Include(p => p.Lancamentos)
                    .FirstOrDefaultAsync(p => p.ID == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar produto por ID: {ID}", id);
                return null;
            }
        }

        #endregion

        #region Métodos de Busca

        public async Task<List<Produto>> ListarTodosAsync()
        {
            try
            {
                return await _context.Produtos
                    .OrderBy(p => p.Categoria)
                    .ThenBy(p => p.Descricao)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar todos os produtos");
                return new List<Produto>();
            }
        }

        public async Task<List<Produto>> ListarAtivosAsync()
        {
            try
            {
                return await _context.Produtos
                    .Where(p => p.Status == true)
                    .OrderBy(p => p.Categoria)
                    .ThenBy(p => p.Descricao)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar produtos ativos");
                return new List<Produto>();
            }
        }

        public async Task<List<Produto>> ListarInativosAsync()
        {
            try
            {
                return await _context.Produtos
                    .Where(p => !p.IsAtivo())
                    .OrderBy(p => p.Categoria)
                    .ThenBy(p => p.Descricao)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar produtos inativos");
                return new List<Produto>();
            }
        }

        public async Task<List<Produto>> BuscarPorCategoriaAsync(string categoria)
        {
            try
            {
                return await _context.Produtos
                    .Where(p => p.Categoria == categoria && p.IsAtivo())
                    .OrderBy(p => p.Descricao)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar produtos por categoria: {Categoria}", categoria);
                return new List<Produto>();
            }
        }

        public async Task<List<Produto>> BuscarPorTextoAsync(string texto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(texto))
                    return new List<Produto>();

                return await _context.Produtos
                    .Where(p => (p.Descricao.Contains(texto) || p.Categoria.Contains(texto)) && p.IsAtivo())
                    .OrderBy(p => p.Categoria)
                    .ThenBy(p => p.Descricao)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar produtos por texto: {Texto}", texto);
                return new List<Produto>();
            }
        }

        public async Task<List<Produto>> BuscarPorFaixaPrecoAsync(decimal precoMin, decimal precoMax)
        {
            try
            {
                return await _context.Produtos
                    .Where(p => p.Valor >= precoMin && p.Valor <= precoMax && p.IsAtivo())
                    .OrderBy(p => p.Valor)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar produtos por faixa de preço: {Min:C} - {Max:C}", precoMin, precoMax);
                return new List<Produto>();
            }
        }

        #endregion

        #region Métodos de Validação

        public async Task<bool> ProdutoJaExisteAsync(string descricao)
        {
            try
            {
                return await _context.Produtos.AnyAsync(p => p.Descricao == descricao);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar se produto já existe: {Descricao}", descricao);
                return true; // Retornar true em caso de erro para evitar duplicatas
            }
        }

        public async Task<bool> PodeInativarAsync(int id)
        {
            try
            {
                var produto = await _context.Produtos.FindAsync(id);
                if (produto == null || !produto.IsAtivo())
                    return false;

                // Verificar se tem lançamentos ativos recentes (últimos 30 dias)
                var temLancamentosRecentes = await _context.LancamentosConsumo
                    .AnyAsync(l => l.ProdutoID == id &&
                                 l.IsAtivo() &&
                                 l.DataHoraLancamento >= DateTime.Now.AddDays(-30));

                return !temLancamentosRecentes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar se pode inativar produto: {ID}", id);
                return false;
            }
        }

        public async Task<bool> TemLancamentosAtivosAsync(int id)
        {
            try
            {
                return await _context.LancamentosConsumo
                    .AnyAsync(l => l.ProdutoID == id && l.IsAtivo());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar lançamentos ativos do produto: {ID}", id);
                return false;
            }
        }

        #endregion

        #region Métodos de Categorias

        public async Task<List<string>> ListarCategoriasAsync()
        {
            try
            {
                return await _context.Produtos
                    .Where(p => p.IsAtivo())
                    .Select(p => p.Categoria)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar categorias");
                return new List<string> { "Bebidas", "Comidas", "Serviços" }; // Categorias padrão
            }
        }

        public async Task<List<Produto>> ListarBebidasAsync()
        {
            return await BuscarPorCategoriaAsync("Bebidas");
        }

        public async Task<List<Produto>> ListarComidasAsync()
        {
            return await BuscarPorCategoriaAsync("Comidas");
        }

        public async Task<List<Produto>> ListarServicosAsync()
        {
            return await BuscarPorCategoriaAsync("Serviços");
        }

        #endregion

        #region Métodos de Relatórios

        public async Task<Dictionary<string, object>> ObterEstatisticasAsync()
        {
            try
            {
                var estatisticas = new Dictionary<string, object>
                {
                    ["TotalProdutos"] = await _context.Produtos.CountAsync(),
                    ["ProdutosAtivos"] = await _context.Produtos.CountAsync(p => p.IsAtivo()),
                    ["ProdutosInativos"] = await _context.Produtos.CountAsync(p => !p.IsAtivo()),
                    ["ValorMedio"] = await CalcularValorMedioAsync(),
                    ["ProdutoMaisCaro"] = await _context.Produtos.Where(p => p.IsAtivo()).OrderByDescending(p => p.Valor).FirstOrDefaultAsync(),
                    ["ProdutoMaisBarato"] = await _context.Produtos.Where(p => p.IsAtivo()).OrderBy(p => p.Valor).FirstOrDefaultAsync(),
                    ["CategoriasBebidas"] = await ContarPorCategoriaAsync("Bebidas"),
                    ["CategoriasComidas"] = await ContarPorCategoriaAsync("Comidas"),
                    ["CategoriasServicos"] = await ContarPorCategoriaAsync("Serviços")
                };

                return estatisticas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter estatísticas de produtos");
                return new Dictionary<string, object>();
            }
        }

        public async Task<List<Produto>> ListarMaisVendidosAsync(int quantidade = 10)
        {
            try
            {
                return await _context.Produtos
                    .Where(p => p.IsAtivo())
                    .Select(p => new {
                        Produto = p,
                        TotalVendido = p.Lancamentos.Where(l => l.IsAtivo()).Sum(l => l.Quantidade)
                    })
                    .OrderByDescending(x => x.TotalVendido)
                    .Take(quantidade)
                    .Select(x => x.Produto)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar produtos mais vendidos");
                return new List<Produto>();
            }
        }

        public async Task<List<Produto>> ListarMenosVendidosAsync(int quantidade = 10)
        {
            try
            {
                return await _context.Produtos
                    .Where(p => p.IsAtivo())
                    .Select(p => new {
                        Produto = p,
                        TotalVendido = p.Lancamentos.Where(l => l.IsAtivo()).Sum(l => l.Quantidade)
                    })
                    .OrderBy(x => x.TotalVendido)
                    .Take(quantidade)
                    .Select(x => x.Produto)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar produtos menos vendidos");
                return new List<Produto>();
            }
        }

        public async Task<decimal> CalcularValorMedioAsync()
        {
            try
            {
                return await _context.Produtos
                    .Where(p => p.IsAtivo())
                    .AverageAsync(p => p.Valor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao calcular valor médio dos produtos");
                return 0;
            }
        }

        #endregion

        #region Métodos Utilitários

        public async Task<List<Produto>> BuscarComFiltrosAsync(string? descricao = null, string? categoria = null,
            bool? ativo = null, decimal? precoMin = null, decimal? precoMax = null)
        {
            try
            {
                var query = _context.Produtos.AsQueryable();

                if (!string.IsNullOrWhiteSpace(descricao))
                    query = query.Where(p => p.Descricao.Contains(descricao));

                if (!string.IsNullOrWhiteSpace(categoria))
                    query = query.Where(p => p.Categoria == categoria);

                if (ativo.HasValue)
                    query = query.Where(p => p.Status == ativo.Value);

                if (precoMin.HasValue)
                    query = query.Where(p => p.Valor >= precoMin.Value);

                if (precoMax.HasValue)
                    query = query.Where(p => p.Valor <= precoMax.Value);

                return await query
                    .OrderBy(p => p.Categoria)
                    .ThenBy(p => p.Descricao)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro na busca com filtros de produtos");
                return new List<Produto>();
            }
        }

        public async Task<int> ContarPorCategoriaAsync(string categoria)
        {
            try
            {
                return await _context.Produtos
                    .CountAsync(p => p.Categoria == categoria && p.IsAtivo());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao contar produtos por categoria: {Categoria}", categoria);
                return 0;
            }
        }

        public async Task<Dictionary<string, int>> ContarPorTodasCategoriasAsync()
        {
            try
            {
                return await _context.Produtos
                    .Where(p => p.IsAtivo())
                    .GroupBy(p => p.Categoria)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao contar produtos por todas as categorias");
                return new Dictionary<string, int>();
            }
        }

        #endregion
    }
}