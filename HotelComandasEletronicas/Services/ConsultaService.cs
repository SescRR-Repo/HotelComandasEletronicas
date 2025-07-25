using Microsoft.EntityFrameworkCore;
using HotelComandasEletronicas.Data;
using HotelComandasEletronicas.Models;
using HotelComandasEletronicas.ViewModels;
using System.Text.RegularExpressions;

namespace HotelComandasEletronicas.Services
{
    public class ConsultaService : IConsultaService
    {
        private readonly ComandasDbContext _context;
        private readonly ILogger<ConsultaService> _logger;

        public ConsultaService(ComandasDbContext context, ILogger<ConsultaService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Métodos de Consulta Pública

        public async Task<ConsultaResultadoViewModel?> ConsultarPorQuartoAsync(string numeroQuarto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(numeroQuarto))
                    return null;

                var registro = await _context.RegistrosHospede
                    .Include(r => r.Lancamentos)
                    .ThenInclude(l => l.Produto)
                    .Where(r => r.NumeroQuarto == numeroQuarto && r.Status == "Ativo")
                    .FirstOrDefaultAsync();

                if (registro == null)
                {
                    _logger.LogWarning("Consulta por quarto não encontrado: {Quarto}", numeroQuarto);
                    return null;
                }

                var resultado = new ConsultaResultadoViewModel
                {
                    RegistroId = registro.ID,
                    NumeroQuarto = registro.NumeroQuarto,
                    NomeCliente = registro.NomeCliente,
                    TelefoneCliente = MascararTelefone(registro.TelefoneCliente),
                    DataCheckIn = registro.DataRegistro,
                    ValorTotalGasto = registro.ValorGastoTotal,
                    Status = registro.Status,
                    DiasHospedado = (DateTime.Now - registro.DataRegistro).Days,
                    TotalItensConsumidos = registro.Lancamentos?.Count(l => l.IsAtivo()) ?? 0,
                    MetodoConsulta = "Quarto"
                };

                _logger.LogInformation("Consulta por quarto realizada: {Quarto}", numeroQuarto);
                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar por quarto: {Quarto}", numeroQuarto);
                return null;
            }
        }

        public async Task<ConsultaResultadoViewModel?> ConsultarPorNomeETelefoneAsync(string nome, string telefone)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(telefone))
                {
                    _logger.LogWarning("Consulta por nome e telefone com dados vazios");
                    return null;
                }

                // Limpar formatação do telefone do usuário
                var telefoneLimpo = Regex.Replace(telefone, @"[^\d]", "");
                if (telefoneLimpo.Length < 8)
                {
                    _logger.LogWarning("Telefone muito curto após limpeza: {TelefoneLimpo}", telefoneLimpo);
                    return null;
                }

                // Limpar nome (remover espaços extras)
                var nomeLimpo = nome.Trim();

                _logger.LogInformation("Buscando por nome: '{Nome}' e telefone: '{Telefone}' (limpo: '{TelefoneLimpo}')", 
                    nomeLimpo, telefone, telefoneLimpo);

                // Buscar com diferentes estratégias
                var registro = await BuscarRegistroPorNomeTelefone(nomeLimpo, telefoneLimpo);

                if (registro == null)
                {
                    _logger.LogWarning("Consulta por nome e telefone não encontrada: Nome='{Nome}', TelefoneOriginal='{Telefone}', TelefoneLimpo='{TelefoneLimpo}'", 
                        nomeLimpo, telefone, telefoneLimpo);
                    return null;
                }

                var resultado = new ConsultaResultadoViewModel
                {
                    RegistroId = registro.ID,
                    NumeroQuarto = registro.NumeroQuarto,
                    NomeCliente = registro.NomeCliente,
                    TelefoneCliente = MascararTelefone(registro.TelefoneCliente),
                    DataCheckIn = registro.DataRegistro,
                    ValorTotalGasto = registro.ValorGastoTotal,
                    Status = registro.Status,
                    DiasHospedado = (DateTime.Now - registro.DataRegistro).Days,
                    TotalItensConsumidos = registro.Lancamentos?.Count(l => l.IsAtivo()) ?? 0,
                    MetodoConsulta = "Nome + Telefone"
                };

                _logger.LogInformation("Consulta por nome e telefone realizada com sucesso: {Nome} - Quarto {Quarto}", 
                    registro.NomeCliente, registro.NumeroQuarto);
                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar por nome e telefone: {Nome}", nome);
                return null;
            }
        }

        private async Task<RegistroHospede?> BuscarRegistroPorNomeTelefone(string nome, string telefoneLimpo)
        {
            // Estratégia 1: Busca exata pelo nome e telefone limpo
            var registro = await _context.RegistrosHospede
                .Include(r => r.Lancamentos)
                .ThenInclude(l => l.Produto)
                .Where(r => r.NomeCliente.ToLower().Contains(nome.ToLower()) && 
                           r.TelefoneCliente.Contains(telefoneLimpo) && 
                           r.Status == "Ativo")
                .FirstOrDefaultAsync();

            if (registro != null)
            {
                _logger.LogInformation("Encontrado com estratégia 1 (nome parcial + telefone limpo)");
                return registro;
            }

            // Estratégia 2: Busca com nome exato e telefone limpo
            registro = await _context.RegistrosHospede
                .Include(r => r.Lancamentos)
                .ThenInclude(l => l.Produto)
                .Where(r => r.NomeCliente.ToLower() == nome.ToLower() && 
                           r.TelefoneCliente.Contains(telefoneLimpo) && 
                           r.Status == "Ativo")
                .FirstOrDefaultAsync();

            if (registro != null)
            {
                _logger.LogInformation("Encontrado com estratégia 2 (nome exato + telefone limpo)");
                return registro;
            }

            // Estratégia 3: Busca mais flexível - limpar telefone do banco também
            var registros = await _context.RegistrosHospede
                .Include(r => r.Lancamentos)
                .ThenInclude(l => l.Produto)
                .Where(r => r.Status == "Ativo")
                .ToListAsync();

            registro = registros.FirstOrDefault(r => 
            {
                var telefoneDbLimpo = Regex.Replace(r.TelefoneCliente, @"[^\d]", "");
                var nomeMatch = r.NomeCliente.ToLower().Contains(nome.ToLower()) || 
                               nome.ToLower().Contains(r.NomeCliente.ToLower());
                var telefoneMatch = telefoneDbLimpo.Contains(telefoneLimpo) || 
                                   telefoneLimpo.Contains(telefoneDbLimpo);
                
                return nomeMatch && telefoneMatch;
            });

            if (registro != null)
            {
                _logger.LogInformation("Encontrado com estratégia 3 (busca flexível em memória)");
                return registro;
            }

            // Estratégia 4: Busca apenas pelos últimos dígitos do telefone
            if (telefoneLimpo.Length >= 8)
            {
                var ultimosDigitos = telefoneLimpo.Substring(telefoneLimpo.Length - 8);
                registro = await _context.RegistrosHospede
                    .Include(r => r.Lancamentos)
                    .ThenInclude(l => l.Produto)
                    .Where(r => r.NomeCliente.ToLower().Contains(nome.ToLower()) && 
                               r.TelefoneCliente.Contains(ultimosDigitos) && 
                               r.Status == "Ativo")
                    .FirstOrDefaultAsync();

                if (registro != null)
                {
                    _logger.LogInformation("Encontrado com estratégia 4 (nome + últimos 8 dígitos do telefone)");
                    return registro;
                }
            }

            _logger.LogWarning("Nenhuma estratégia de busca encontrou resultado para: Nome='{Nome}', Telefone='{Telefone}'", 
                nome, telefoneLimpo);
            return null;
        }

        #endregion

        #region Métodos de Extrato

        public async Task<ExtratoConsultaViewModel> GerarExtratoCompletoAsync(int registroHospedeId)
        {
            try
            {
                var registro = await _context.RegistrosHospede
                    .Include(r => r.Lancamentos)
                    .ThenInclude(l => l.Produto)
                    .FirstOrDefaultAsync(r => r.ID == registroHospedeId);

                if (registro == null)
                    return new ExtratoConsultaViewModel();

                var consumos = registro.Lancamentos?.Where(l => l.IsAtivo()).ToList() ?? new List<LancamentoConsumo>();

                var extrato = new ExtratoConsultaViewModel
                {
                    RegistroId = registro.ID,
                    NumeroQuarto = registro.NumeroQuarto,
                    NomeCliente = registro.NomeCliente,
                    DataCheckIn = registro.DataRegistro,
                    DiasHospedado = (DateTime.Now - registro.DataRegistro).Days,
                    ValorTotalGasto = registro.ValorGastoTotal,
                    TotalItens = consumos.Count,
                    Consumos = consumos.OrderByDescending(l => l.DataHoraLancamento).ToList(),
                    
                    // Estatísticas por categoria
                    TotalBebidas = consumos.Where(l => l.Produto?.IsCategoria("Bebidas") == true).Sum(l => l.ValorTotal),
                    TotalComidas = consumos.Where(l => l.Produto?.IsCategoria("Comidas") == true).Sum(l => l.ValorTotal),
                    TotalServicos = consumos.Where(l => l.Produto?.IsCategoria("Serviços") == true).Sum(l => l.ValorTotal),
                    
                    // Estatísticas gerais
                    UltimoConsumo = consumos.OrderByDescending(l => l.DataHoraLancamento).FirstOrDefault()?.DataHoraLancamento,
                    MediaValorPorItem = consumos.Any() ? consumos.Average(l => l.ValorTotal) : 0,
                    DataGeracaoExtrato = DateTime.Now
                };

                return extrato;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar extrato completo: {RegistroId}", registroHospedeId);
                return new ExtratoConsultaViewModel();
            }
        }

        public async Task<List<LancamentoConsumo>> ObterConsumosAsync(int registroHospedeId)
        {
            try
            {
                return await _context.LancamentosConsumo
                    .Include(l => l.Produto)
                    .Where(l => l.RegistroHospedeID == registroHospedeId && l.Status == "Ativo")
                    .OrderByDescending(l => l.DataHoraLancamento)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter consumos: {RegistroId}", registroHospedeId);
                return new List<LancamentoConsumo>();
            }
        }

        #endregion

        #region Métodos de Validação

        public async Task<bool> ValidarAcessoConsultaAsync(string numeroQuarto)
        {
            try
            {
                return await _context.RegistrosHospede
                    .AnyAsync(r => r.NumeroQuarto == numeroQuarto && r.Status == "Ativo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar acesso consulta: {Quarto}", numeroQuarto);
                return false;
            }
        }

        public async Task<bool> ValidarNomeTelefoneAsync(string nome, string telefone, string numeroQuarto)
        {
            try
            {
                var telefoneLimpo = Regex.Replace(telefone, @"[^\d]", "");
                
                return await _context.RegistrosHospede
                    .AnyAsync(r => r.NumeroQuarto == numeroQuarto &&
                                 r.NomeCliente.Contains(nome) &&
                                 r.TelefoneCliente.Contains(telefoneLimpo) &&
                                 r.Status == "Ativo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar nome e telefone");
                return false;
            }
        }

        public async Task<bool> RegistroEstaAtivoAsync(int registroHospedeId)
        {
            try
            {
                var registro = await _context.RegistrosHospede.FindAsync(registroHospedeId);
                return registro?.IsAtivo() ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar se registro está ativo: {Id}", registroHospedeId);
                return false;
            }
        }

        #endregion

        #region Métodos de Segurança

        public string MascararTelefone(string telefone)
        {
            if (string.IsNullOrWhiteSpace(telefone) || telefone.Length < 8)
                return "***-****";

            // Remover formatação
            var telefoneLimpo = Regex.Replace(telefone, @"[^\d]", "");
            
            if (telefoneLimpo.Length == 11) // Celular com DDD
            {
                return $"({telefoneLimpo.Substring(0, 2)}) {telefoneLimpo.Substring(2, 1)}****-{telefoneLimpo.Substring(7, 4)}";
            }
            else if (telefoneLimpo.Length == 10) // Fixo com DDD
            {
                return $"({telefoneLimpo.Substring(0, 2)}) ****-{telefoneLimpo.Substring(6, 4)}";
            }
            
            return "***-****";
        }

        public bool ValidarFormatoTelefone(string telefone)
        {
            if (string.IsNullOrWhiteSpace(telefone))
                return false;

            var telefoneLimpo = Regex.Replace(telefone, @"[^\d]", "");
            return telefoneLimpo.Length >= 8 && telefoneLimpo.Length <= 11;
        }

        #endregion

        #region Métodos Utilitários

        public async Task<Dictionary<string, object>> ObterEstatisticasConsultaAsync(int registroHospedeId)
        {
            try
            {
                var consumos = await ObterConsumosAsync(registroHospedeId);
                
                return new Dictionary<string, object>
                {
                    ["TotalItens"] = consumos.Count,
                    ["ValorTotal"] = consumos.Sum(c => c.ValorTotal),
                    ["MediaValor"] = consumos.Any() ? consumos.Average(c => c.ValorTotal) : 0,
                    ["UltimoConsumo"] = consumos.OrderByDescending(c => c.DataHoraLancamento).FirstOrDefault()?.DataHoraLancamento,
                    ["ProdutoMaisConsumido"] = consumos.GroupBy(c => c.Produto?.Descricao)
                                                     .OrderByDescending(g => g.Count())
                                                     .FirstOrDefault()?.Key ?? "Nenhum"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter estatísticas de consulta: {RegistroId}", registroHospedeId);
                return new Dictionary<string, object>();
            }
        }

        public string GerarResumoConsulta(ConsultaResultadoViewModel resultado)
        {
            if (resultado == null)
                return "Consulta sem resultados";

            return $"Hóspede: {resultado.NomeCliente} | " +
                   $"Quarto: {resultado.NumeroQuarto} | " +
                   $"Check-in: {resultado.DataCheckIn:dd/MM/yyyy} | " +
                   $"Total gasto: {resultado.ValorTotalGasto:C2} | " +
                   $"Itens: {resultado.TotalItensConsumidos}";
        }

        #endregion
    }
}
