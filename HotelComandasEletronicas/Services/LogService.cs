using Microsoft.EntityFrameworkCore;
using HotelComandasEletronicas.Data;
using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.Services
{
    public class LogService : ILogService
    {
        private readonly IDbContextFactory<ComandasDbContext> _contextFactory;
        private readonly ILogger<LogService> _logger;

        public LogService(IDbContextFactory<ComandasDbContext> contextFactory, ILogger<LogService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        // Implementações básicas - serão desenvolvidas posteriormente
        public async Task RegistrarLogAsync(string codigoUsuario, string acao, string tabela,
            int? registroId = null, string? detalhesAntes = null, string? detalhesDepois = null)
        {
            try
            {
                // Usar uma nova instância do contexto para evitar conflitos
                using var context = await _contextFactory.CreateDbContextAsync();
                
                var log = new LogSistema();
                log.RegistrarAcao(codigoUsuario, acao, tabela, registroId, detalhesAntes, detalhesDepois);

                context.LogsSistema.Add(log);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log apenas no console/arquivo para evitar loop infinito
                _logger.LogError(ex, "Erro ao registrar log no banco de dados");
            }
        }

        public async Task RegistrarLoginAsync(string codigoUsuario, bool sucesso, string? detalhes = null)
        {
            await RegistrarLogAsync(codigoUsuario, "Login", "USUARIOS_SISTEMA", null, null, detalhes);
        }

        public async Task RegistrarAcaoAsync(string codigoUsuario, string acao, string detalhes)
        {
            await RegistrarLogAsync(codigoUsuario, acao, "SISTEMA", null, null, detalhes);
        }

        public async Task<List<LogSistema>> BuscarLogsPorUsuarioAsync(string codigoUsuario, DateTime? inicio = null, DateTime? fim = null)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var query = context.LogsSistema.Where(l => l.CodigoUsuario == codigoUsuario);

            if (inicio.HasValue) query = query.Where(l => l.DataHora >= inicio.Value);
            if (fim.HasValue) query = query.Where(l => l.DataHora <= fim.Value);

            return await query.OrderByDescending(l => l.DataHora).ToListAsync();
        }

        public async Task<List<LogSistema>> BuscarLogsPorAcaoAsync(string acao, DateTime? inicio = null, DateTime? fim = null)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var query = context.LogsSistema.Where(l => l.Acao == acao);

            if (inicio.HasValue) query = query.Where(l => l.DataHora >= inicio.Value);
            if (fim.HasValue) query = query.Where(l => l.DataHora <= fim.Value);

            return await query.OrderByDescending(l => l.DataHora).ToListAsync();
        }

        public async Task<List<LogSistema>> BuscarLogsPorPeriodoAsync(DateTime inicio, DateTime fim)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.LogsSistema
                .Where(l => l.DataHora >= inicio && l.DataHora <= fim)
                .OrderByDescending(l => l.DataHora)
                .ToListAsync();
        }

        public async Task<List<LogSistema>> GetLogsAuditoriaAsync(int quantidade = 100)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.LogsSistema
                .OrderByDescending(l => l.DataHora)
                .Take(quantidade)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetEstatisticasLogsAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.LogsSistema
                .GroupBy(l => l.Acao)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        public async Task<bool> LimparLogsAntigosAsync(int diasParaManter = 90)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var dataLimite = DateTime.Now.AddDays(-diasParaManter);
            var logsAntigos = context.LogsSistema.Where(l => l.DataHora < dataLimite);

            context.LogsSistema.RemoveRange(logsAntigos);
            await context.SaveChangesAsync();

            return true;
        }

        public async Task<string> ObterIPAddressAsync()
        {
            await Task.CompletedTask;
            return "127.0.0.1"; // Implementar captura real do IP
        }

        public string FormatarDetalhesJson(object objeto)
        {
            return System.Text.Json.JsonSerializer.Serialize(objeto);
        }
    }
}