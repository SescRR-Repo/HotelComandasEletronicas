using Microsoft.EntityFrameworkCore;
using HotelComandasEletronicas.Data;
using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.Services
{
    public class LancamentoService : ILancamentoService
    {
        private readonly ComandasDbContext _context;
        private readonly ILogger<LancamentoService> _logger;

        public LancamentoService(ComandasDbContext context, ILogger<LancamentoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Métodos de CRUD

        public async Task<bool> RegistrarConsumoAsync(LancamentoConsumo lancamento)
        {
            try
            {
                // Validações
                if (lancamento.Quantidade <= 0)
                {
                    _logger.LogWarning("Tentativa de lançamento com quantidade inválida: {Quantidade}", lancamento.Quantidade);
                    return false;
                }

                if (lancamento.ValorUnitario <= 0 || lancamento.ValorTotal <= 0)
                {
                    _logger.LogWarning("Tentativa de lançamento com valores inválidos");
                    return false;
                }

                // Usar uma transação para todo o processo
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Verificar se produto existe e está ativo
                    var produto = await _context.Produtos.FindAsync(lancamento.ProdutoID);
                    if (produto == null || !produto.IsAtivo())
                    {
                        _logger.LogWarning("Tentativa de lançamento para produto inexistente ou inativo: {ProdutoID}", lancamento.ProdutoID);
                        await transaction.RollbackAsync();
                        return false;
                    }

                    // Verificar se hóspede existe e está ativo
                    var hospede = await _context.RegistrosHospede.FindAsync(lancamento.RegistroHospedeID);
                    if (hospede == null || !hospede.IsAtivo())
                    {
                        _logger.LogWarning("Tentativa de lançamento para hóspede inexistente ou inativo: {HospedeID}", lancamento.RegistroHospedeID);
                        await transaction.RollbackAsync();
                        return false;
                    }

                    // Registrar lançamento
                    _context.LancamentosConsumo.Add(lancamento);
                    await _context.SaveChangesAsync();

                    // Atualizar valor gasto total do hóspede na mesma transação
                    var valorTotal = await CalcularTotalHospedeAsync(lancamento.RegistroHospedeID);
                    hospede.ValorGastoTotal = valorTotal;
                    await _context.SaveChangesAsync();

                    // Commit da transação
                    await transaction.CommitAsync();

                    _logger.LogInformation("Lançamento registrado com sucesso: ID {LancamentoID}, Produto {ProdutoID}, Valor {Valor}",
                        lancamento.ID, lancamento.ProdutoID, lancamento.ValorTotal);

                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Erro durante a transação de lançamento");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar consumo");
                return false;
            }
        }

        public async Task<bool> CancelarLancamentoAsync(int id, string motivo, string usuarioCancelamento)
        {
            try
            {
                var lancamento = await _context.LancamentosConsumo.FindAsync(id);
                if (lancamento == null)
                {
                    _logger.LogWarning("Tentativa de cancelar lançamento inexistente: {LancamentoID}", id);
                    return false;
                }

                if (!lancamento.IsAtivo())
                {
                    _logger.LogWarning("Tentativa de cancelar lançamento já cancelado: {LancamentoID}", id);
                    return false;
                }

                // Cancelar lançamento
                lancamento.Status = "Cancelado";
                lancamento.DataCancelamento = DateTime.Now;
                lancamento.ObservacoesCancelamento = motivo;
                lancamento.UsuarioCancelamento = usuarioCancelamento;

                await _context.SaveChangesAsync();

                // Atualizar valor gasto total do hóspede
                await AtualizarValorGastoHospedeAsync(lancamento.RegistroHospedeID);

                _logger.LogInformation("Lançamento cancelado: ID {LancamentoID} por {Usuario}. Motivo: {Motivo}",
                    id, usuarioCancelamento, motivo);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cancelar lançamento ID: {LancamentoID}", id);
                return false;
            }
        }

        public async Task<bool> RemoverItemCarrinhoAsync(int id)
        {
            try
            {
                var lancamento = await _context.LancamentosConsumo.FindAsync(id);
                if (lancamento == null)
                {
                    return false;
                }

                // Se já foi processado (tem data de lançamento antiga), não pode ser removido
                if (lancamento.DataHoraLancamento < DateTime.Now.AddMinutes(-5))
                {
                    _logger.LogWarning("Tentativa de remover lançamento antigo: {LancamentoID}", id);
                    return false;
                }

                var hospedeId = lancamento.RegistroHospedeID;

                _context.LancamentosConsumo.Remove(lancamento);
                await _context.SaveChangesAsync();

                // Atualizar valor gasto total do hóspede
                await AtualizarValorGastoHospedeAsync(hospedeId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao remover item do carrinho ID: {LancamentoID}", id);
                return false;
            }
        }

        public async Task<LancamentoConsumo?> BuscarPorIdAsync(int id)
        {
            try
            {
                return await _context.LancamentosConsumo
                    .Include(l => l.Produto)
                    .Include(l => l.RegistroHospede)
                    .FirstOrDefaultAsync(l => l.ID == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar lançamento por ID: {LancamentoID}", id);
                return null;
            }
        }

        #endregion

        #region Métodos de Busca

        public async Task<List<LancamentoConsumo>> GetConsumosPorHospedeAsync(int hospedeId)
        {
            try
            {
                return await _context.LancamentosConsumo
                    .Include(l => l.Produto)
                    .Include(l => l.RegistroHospede)
                    .Where(l => l.RegistroHospedeID == hospedeId)
                    .OrderByDescending(l => l.DataHoraLancamento)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar consumos por hóspede: {HospedeID}", hospedeId);
                return new List<LancamentoConsumo>();
            }
        }

        public async Task<List<LancamentoConsumo>> GetConsumosPorPeriodoAsync(DateTime inicio, DateTime fim)
        {
            try
            {
                return await _context.LancamentosConsumo
                    .Include(l => l.Produto)
                    .Include(l => l.RegistroHospede)
                    .Where(l => l.DataHoraLancamento >= inicio && l.DataHoraLancamento <= fim)
                    .OrderByDescending(l => l.DataHoraLancamento)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar consumos por período: {Inicio} - {Fim}", inicio, fim);
                return new List<LancamentoConsumo>();
            }
        }

        public async Task<List<LancamentoConsumo>> GetConsumosPorUsuarioAsync(string codigoUsuario)
        {
            try
            {
                return await _context.LancamentosConsumo
                    .Include(l => l.Produto)
                    .Include(l => l.RegistroHospede)
                    .Where(l => l.CodigoUsuarioLancamento == codigoUsuario)
                    .OrderByDescending(l => l.DataHoraLancamento)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar consumos por usuário: {Usuario}", codigoUsuario);
                return new List<LancamentoConsumo>();
            }
        }

        public async Task<List<LancamentoConsumo>> ListarTodosAsync()
        {
            try
            {
                return await _context.LancamentosConsumo
                    .Include(l => l.Produto)
                    .Include(l => l.RegistroHospede)
                    .OrderByDescending(l => l.DataHoraLancamento)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar todos os lançamentos");
                return new List<LancamentoConsumo>();
            }
        }

        #endregion

        #region Métodos de Cálculo

        public async Task<decimal> CalcularTotalPeriodoAsync(DateTime inicio, DateTime fim)
        {
            try
            {
                return await _context.LancamentosConsumo
                    .Where(l => l.DataHoraLancamento >= inicio && 
                               l.DataHoraLancamento <= fim && 
                               l.Status == "Ativo")
                    .SumAsync(l => l.ValorTotal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao calcular total do período");
                return 0;
            }
        }

        public async Task<decimal> CalcularTotalHospedeAsync(int hospedeId)
        {
            try
            {
                return await _context.LancamentosConsumo
                    .Where(l => l.RegistroHospedeID == hospedeId && l.Status == "Ativo")
                    .SumAsync(l => l.ValorTotal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao calcular total do hóspede: {HospedeID}", hospedeId);
                return 0;
            }
        }

        #endregion

        #region Métodos de Validação

        public async Task<bool> ValidarPermissaoCancelamentoAsync(string codigoUsuario)
        {
            try
            {
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.CodigoID == codigoUsuario);
                
                return usuario?.TemPermissaoCancelamento() ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar permissão de cancelamento para: {Usuario}", codigoUsuario);
                return false;
            }
        }

        public async Task<bool> PodeCancelarAsync(int lancamentoId, string codigoUsuario)
        {
            try
            {
                var lancamento = await _context.LancamentosConsumo.FindAsync(lancamentoId);
                if (lancamento == null || !lancamento.IsAtivo()) 
                    return false;

                // Verificar se tem permissão de cancelamento
                var temPermissao = await ValidarPermissaoCancelamentoAsync(codigoUsuario);
                if (!temPermissao)
                    return false;

                // Verificar se não passou muito tempo desde o lançamento
                var tempoLimite = DateTime.Now.AddHours(-24); // 24 horas
                if (lancamento.DataHoraLancamento < tempoLimite)
                {
                    _logger.LogWarning("Tentativa de cancelar lançamento muito antigo: {LancamentoID}", lancamentoId);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar se pode cancelar lançamento: {LancamentoID}", lancamentoId);
                return false;
            }
        }

        #endregion

        #region Métodos Utilitários

        public async Task<Dictionary<string, object>> ObterEstatisticasAsync()
        {
            try
            {
                var hoje = DateTime.Today;
                var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);

                var stats = new Dictionary<string, object>
                {
                    ["TotalLancamentos"] = await _context.LancamentosConsumo.CountAsync(),
                    ["LancamentosAtivos"] = await _context.LancamentosConsumo.CountAsync(l => l.Status == "Ativo"),
                    ["LancamentosCancelados"] = await _context.LancamentosConsumo.CountAsync(l => l.Status == "Cancelado"),
                    ["ValorTotalGeral"] = await _context.LancamentosConsumo.Where(l => l.Status == "Ativo").SumAsync(l => l.ValorTotal),
                    ["ValorTotalHoje"] = await _context.LancamentosConsumo
                        .Where(l => l.Status == "Ativo" && l.DataHoraLancamento >= hoje)
                        .SumAsync(l => l.ValorTotal),
                    ["ValorTotalMes"] = await _context.LancamentosConsumo
                        .Where(l => l.Status == "Ativo" && l.DataHoraLancamento >= inicioMes)
                        .SumAsync(l => l.ValorTotal),
                    ["LancamentosHoje"] = await _context.LancamentosConsumo
                        .CountAsync(l => l.DataHoraLancamento >= hoje),
                    ["ProdutoMaisVendido"] = await ObterProdutoMaisVendidoAsync(),
                    ["UsuarioMaisAtivo"] = await ObterUsuarioMaisAtivoAsync()
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter estatísticas");
                return new Dictionary<string, object>();
            }
        }

        #endregion

        #region Métodos Privados

        /// <summary>
        /// Atualiza o valor gasto total do hóspede
        /// </summary>
        private async Task AtualizarValorGastoHospedeAsync(int hospedeId)
        {
            try
            {
                // Usar uma transação para evitar problemas de concorrência
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                var hospede = await _context.RegistrosHospede.FindAsync(hospedeId);
                if (hospede == null) 
                {
                    await transaction.RollbackAsync();
                    return;
                }

                var valorTotal = await CalcularTotalHospedeAsync(hospedeId);
                hospede.ValorGastoTotal = valorTotal;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                _logger.LogInformation("Valor gasto atualizado para hóspede {HospedeID}: {Valor:C}", hospedeId, valorTotal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar valor gasto do hóspede: {HospedeID}", hospedeId);
            }
        }

        /// <summary>
        /// Obter produto mais vendido
        /// </summary>
        private async Task<object> ObterProdutoMaisVendidoAsync()
        {
            try
            {
                var produto = await _context.LancamentosConsumo
                    .Where(l => l.Status == "Ativo")
                    .GroupBy(l => l.ProdutoID)
                    .Select(g => new { ProdutoID = g.Key, TotalVendido = g.Sum(l => l.Quantidade) })
                    .OrderByDescending(x => x.TotalVendido)
                    .FirstOrDefaultAsync();

                if (produto == null) return new { Descricao = "Nenhum", Quantidade = 0 };

                var produtoInfo = await _context.Produtos.FindAsync(produto.ProdutoID);
                return new 
                { 
                    Descricao = produtoInfo?.Descricao ?? "Desconhecido", 
                    Quantidade = produto.TotalVendido 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter produto mais vendido");
                return new { Descricao = "Erro", Quantidade = 0 };
            }
        }

        /// <summary>
        /// Obter usuário mais ativo
        /// </summary>
        private async Task<object> ObterUsuarioMaisAtivoAsync()
        {
            try
            {
                var usuario = await _context.LancamentosConsumo
                    .Where(l => l.Status == "Ativo")
                    .GroupBy(l => l.CodigoUsuarioLancamento)
                    .Select(g => new { Codigo = g.Key, TotalLancamentos = g.Count() })
                    .OrderByDescending(x => x.TotalLancamentos)
                    .FirstOrDefaultAsync();

                if (usuario == null) return new { Nome = "Nenhum", Lancamentos = 0 };

                var usuarioInfo = await _context.Usuarios.FirstOrDefaultAsync(u => u.CodigoID == usuario.Codigo);
                return new 
                { 
                    Nome = usuarioInfo?.Nome ?? usuario.Codigo, 
                    Lancamentos = usuario.TotalLancamentos 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter usuário mais ativo");
                return new { Nome = "Erro", Lancamentos = 0 };
            }
        }

        #endregion
    }
}
