using Microsoft.EntityFrameworkCore;
using HotelComandasEletronicas.Data;
using HotelComandasEletronicas.Models;
using System.Text.RegularExpressions;

namespace HotelComandasEletronicas.Services
{
    public class RegistroHospedeService : IRegistroHospedeService
    {
        private readonly ComandasDbContext _context;
        private readonly ILogger<RegistroHospedeService> _logger;

        public RegistroHospedeService(ComandasDbContext context, ILogger<RegistroHospedeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Métodos de CRUD

        public async Task<bool> CriarRegistroAsync(RegistroHospede registro)
        {
            return await RegistrarHospedeAsync(registro);
        }

        public async Task<bool> AlterarRegistroAsync(RegistroHospede registro)
        {
            return await AlterarHospedeAsync(registro);
        }

        public async Task<bool> FinalizarRegistroAsync(int id)
        {
            return await FinalizarRegistroAsync(id, "Sistema");
        }

        public async Task<bool> RegistrarHospedeAsync(RegistroHospede registro)
        {
            try
            {
                // Validações
                if (await QuartoJaExisteAsync(registro.NumeroQuarto))
                {
                    _logger.LogWarning("Tentativa de registrar hóspede em quarto já ocupado: {Quarto}", registro.NumeroQuarto);
                    return false;
                }

                registro.DataRegistro = DateTime.Now;
                registro.Status = "Ativo";
                registro.ValorGastoTotal = 0.00m;

                _context.RegistrosHospede.Add(registro);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Hóspede registrado com sucesso: {Nome} - Quarto {Quarto}",
                    registro.NomeCliente, registro.NumeroQuarto);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar hóspede: {Nome}", registro.NomeCliente);
                return false;
            }
        }

        public async Task<bool> AlterarHospedeAsync(RegistroHospede registro)
        {
            try
            {
                var registroExistente = await _context.RegistrosHospede.FindAsync(registro.ID);
                if (registroExistente == null)
                    return false;

                // Verificar se o novo quarto já existe (exceto o próprio registro)
                if (registro.NumeroQuarto != registroExistente.NumeroQuarto)
                {
                    var quartoExiste = await _context.RegistrosHospede
                        .AnyAsync(r => r.NumeroQuarto == registro.NumeroQuarto && r.ID != registro.ID && r.Status == "Ativo");
                    if (quartoExiste)
                        return false;
                }

                // Atualizar campos
                registroExistente.NumeroQuarto = registro.NumeroQuarto;
                registroExistente.NomeCliente = registro.NomeCliente;
                registroExistente.TelefoneCliente = registro.TelefoneCliente;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Registro de hóspede alterado: {Nome} (ID: {ID})", registro.NomeCliente, registro.ID);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao alterar registro de hóspede: {ID}", registro.ID);
                return false;
            }
        }

        public async Task<bool> FinalizarRegistroAsync(int id, string usuarioFinalizacao)
        {
            try
            {
                var registro = await _context.RegistrosHospede.FindAsync(id);
                if (registro == null || !registro.IsAtivo())
                    return false;

                registro.Finalizar();
                await _context.SaveChangesAsync();

                _logger.LogInformation("Registro finalizado: {Nome} - Quarto {Quarto} por {Usuario}",
                    registro.NomeCliente, registro.NumeroQuarto, usuarioFinalizacao);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao finalizar registro: {ID}", id);
                return false;
            }
        }

        #endregion

        #region Métodos de Busca

        public async Task<RegistroHospede?> BuscarPorIdAsync(int id)
        {
            try
            {
                return await _context.RegistrosHospede
                    .Include(r => r.Lancamentos)
                    .ThenInclude(l => l.Produto)
                    .FirstOrDefaultAsync(r => r.ID == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar hóspede por ID: {ID}", id);
                return null;
            }
        }

        public async Task<RegistroHospede?> BuscarPorQuartoAsync(string numeroQuarto)
        {
            try
            {
                return await _context.RegistrosHospede
                    .Include(r => r.Lancamentos)
                    .ThenInclude(l => l.Produto)
                    .FirstOrDefaultAsync(r => r.NumeroQuarto == numeroQuarto && r.Status == "Ativo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar hóspede por quarto: {Quarto}", numeroQuarto);
                return null;
            }
        }

        public async Task<RegistroHospede?> BuscarPorNomeETelefoneAsync(string nome, string telefone)
        {
            try
            {
                return await _context.RegistrosHospede
                    .Include(r => r.Lancamentos)
                    .ThenInclude(l => l.Produto)
                    .FirstOrDefaultAsync(r => r.NomeCliente.Contains(nome) &&
                                            r.TelefoneCliente.Contains(telefone) &&
                                            r.Status == "Ativo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar hóspede por nome e telefone: {Nome}, {Telefone}", nome, telefone);
                return null;
            }
        }

        #endregion

        #region Busca Inteligente

        public async Task<List<RegistroHospede>> BuscarPorTextoAsync(string termo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(termo))
                    return new List<RegistroHospede>();

                var tipoBusca = DetectarTipoBusca(termo);

                return tipoBusca switch
                {
                    "Quarto" => await BuscarPorQuartoSimilarAsync(termo),
                    "Telefone" => await BuscarPorTelefoneAsync(termo),
                    "Nome" => await BuscarPorNomeAsync(termo),
                    _ => await BuscarGeralAsync(termo)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro na busca inteligente por texto: {Termo}", termo);
                return new List<RegistroHospede>();
            }
        }

        public string DetectarTipoBusca(string termo)
        {
            if (string.IsNullOrWhiteSpace(termo))
                return "Geral";

            // Remover espaços e caracteres especiais para análise
            var termoLimpo = termo.Trim();

            // Detectar se é número de quarto (apenas números, ou números com letras no final)
            if (Regex.IsMatch(termoLimpo, @"^\d+[A-Za-z]?$"))
            {
                return "Quarto";
            }

            // Detectar se é telefone (apenas números ou com formatação)
            if (Regex.IsMatch(termoLimpo, @"^[\d\s\(\)\-]+$") && termoLimpo.Length >= 8)
            {
                return "Telefone";
            }

            // Caso contrário, assumir que é nome
            return "Nome";
        }

        private async Task<List<RegistroHospede>> BuscarPorQuartoSimilarAsync(string termo)
        {
            return await _context.RegistrosHospede
                .Where(r => r.NumeroQuarto.Contains(termo) && r.Status == "Ativo")
                .OrderBy(r => r.NumeroQuarto)
                .ToListAsync();
        }

        public async Task<List<RegistroHospede>> BuscarPorNomeAsync(string nome)
        {
            return await _context.RegistrosHospede
                .Where(r => r.NomeCliente.Contains(nome) && r.Status == "Ativo")
                .OrderBy(r => r.NomeCliente)
                .ToListAsync();
        }

        public async Task<List<RegistroHospede>> BuscarPorTelefoneAsync(string telefone)
        {
            // Limpar formatação do telefone para busca mais flexível
            var telefoneLimpo = Regex.Replace(telefone, @"[^\d]", "");

            return await _context.RegistrosHospede
                .Where(r => r.TelefoneCliente.Contains(telefoneLimpo) && r.Status == "Ativo")
                .OrderBy(r => r.NomeCliente)
                .ToListAsync();
        }

        private async Task<List<RegistroHospede>> BuscarGeralAsync(string termo)
        {
            return await _context.RegistrosHospede
                .Where(r => (r.NomeCliente.Contains(termo) ||
                           r.NumeroQuarto.Contains(termo) ||
                           r.TelefoneCliente.Contains(termo)) && r.Status == "Ativo")
                .OrderBy(r => r.NomeCliente)
                .ToListAsync();
        }

        #endregion

        #region Métodos de Consulta

        public async Task<List<RegistroHospede>> ListarTodosAsync()
        {
            try
            {
                return await _context.RegistrosHospede
                    .Include(r => r.Lancamentos)
                    .OrderByDescending(r => r.DataRegistro)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar todos os registros de hóspedes");
                return new List<RegistroHospede>();
            }
        }

        public async Task<List<RegistroHospede>> ListarAtivosAsync()
        {
            try
            {
                return await _context.RegistrosHospede
                    .Where(r => r.Status == "Ativo")
                    .OrderBy(r => r.NumeroQuarto)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar hóspedes ativos");
                return new List<RegistroHospede>();
            }
        }

        public async Task<List<RegistroHospede>> ListarFinalizadosAsync()
        {
            try
            {
                return await _context.RegistrosHospede
                    .Where(r => r.Status == "Finalizado")
                    .OrderByDescending(r => r.DataRegistro)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar hóspedes finalizados");
                return new List<RegistroHospede>();
            }
        }

        public async Task<List<RegistroHospede>> ListarPorPeriodoAsync(DateTime inicio, DateTime fim)
        {
            try
            {
                return await _context.RegistrosHospede
                    .Where(r => r.DataRegistro >= inicio && r.DataRegistro <= fim)
                    .OrderByDescending(r => r.DataRegistro)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar hóspedes por período: {Inicio} - {Fim}", inicio, fim);
                return new List<RegistroHospede>();
            }
        }

        #endregion

        #region Métodos de Validação

        public async Task<bool> QuartoJaExisteAsync(string numeroQuarto, int? excluirId = null)
        {
            try
            {
                var query = _context.RegistrosHospede
                    .Where(r => r.NumeroQuarto == numeroQuarto && r.Status == "Ativo");

                if (excluirId.HasValue)
                {
                    query = query.Where(r => r.ID != excluirId.Value);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar se quarto já existe: {Quarto}", numeroQuarto);
                return true; // Retornar true em caso de erro para evitar duplicatas
            }
        }

        public async Task<bool> PodeFinalizarAsync(int id)
        {
            try
            {
                var registro = await _context.RegistrosHospede.FindAsync(id);
                return registro?.PodeFinalizar() ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar se pode finalizar: {ID}", id);
                return false;
            }
        }

        public async Task<bool> TemConsumosAtivosAsync(int id)
        {
            try
            {
                return await _context.LancamentosConsumo
                    .AnyAsync(l => l.RegistroHospedeID == id && l.Status == "Ativo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar consumos ativos: {ID}", id);
                return false;
            }
        }

        #endregion

        #region Métodos de Cálculo

        public async Task AtualizarValorGastoAsync(int hospedeId)
        {
            try
            {
                var registro = await _context.RegistrosHospede.FindAsync(hospedeId);
                if (registro == null)
                    return;

                var totalGasto = await _context.LancamentosConsumo
                    .Where(l => l.RegistroHospedeID == hospedeId && l.Status == "Ativo")
                    .SumAsync(l => l.ValorTotal);

                registro.ValorGastoTotal = totalGasto;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Valor gasto atualizado para hóspede {ID}: {Valor:C}", hospedeId, totalGasto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar valor gasto: {HospedeId}", hospedeId);
            }
        }

        public async Task<decimal> CalcularTotalGeralAsync()
        {
            try
            {
                return await _context.RegistrosHospede
                    .Where(r => r.Status == "Ativo")
                    .SumAsync(r => r.ValorGastoTotal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao calcular total geral");
                return 0;
            }
        }

        public async Task<int> ContarHospedesAtivosAsync()
        {
            try
            {
                return await _context.RegistrosHospede
                    .CountAsync(r => r.Status == "Ativo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao contar hóspedes ativos");
                return 0;
            }
        }

        #endregion

        #region Métodos Utilitários

        public async Task<Dictionary<string, object>> ObterEstatisticasAsync()
        {
            try
            {
                var estatisticas = new Dictionary<string, object>
                {
                    ["TotalHospedes"] = await _context.RegistrosHospede.CountAsync(),
                    ["HospedesAtivos"] = await ContarHospedesAtivosAsync(),
                    ["HospedesFinalizados"] = await _context.RegistrosHospede.CountAsync(r => r.Status == "Finalizado"),
                    ["ValorTotalGeral"] = await CalcularTotalGeralAsync(),
                    ["MediaValorPorHospede"] = await _context.RegistrosHospede.Where(r => r.Status == "Ativo").AverageAsync(r => (double?)r.ValorGastoTotal) ?? 0,
                    ["UltimoRegistro"] = await _context.RegistrosHospede.OrderByDescending(r => r.DataRegistro).FirstOrDefaultAsync()
                };

                return estatisticas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter estatísticas");
                return new Dictionary<string, object>();
            }
        }

        public async Task<List<RegistroHospede>> BuscarComFiltrosAsync(string? quarto = null, string? nome = null,
            string? status = null, DateTime? inicio = null, DateTime? fim = null)
        {
            try
            {
                var query = _context.RegistrosHospede.AsQueryable();

                if (!string.IsNullOrWhiteSpace(quarto))
                    query = query.Where(r => r.NumeroQuarto.Contains(quarto));

                if (!string.IsNullOrWhiteSpace(nome))
                    query = query.Where(r => r.NomeCliente.Contains(nome));

                if (!string.IsNullOrWhiteSpace(status))
                    query = query.Where(r => r.Status == status);

                if (inicio.HasValue)
                    query = query.Where(r => r.DataRegistro >= inicio.Value);

                if (fim.HasValue)
                    query = query.Where(r => r.DataRegistro <= fim.Value);

                return await query
                    .OrderByDescending(r => r.DataRegistro)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro na busca com filtros");
                return new List<RegistroHospede>();
            }
        }

        #endregion
    }
}