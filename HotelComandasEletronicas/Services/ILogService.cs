using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.Services
{
    public interface ILogService
    {
        // Métodos de registro de log
        Task RegistrarLogAsync(string codigoUsuario, string acao, string tabela,
            int? registroId = null, string? detalhesAntes = null, string? detalhesDepois = null);
        Task RegistrarLoginAsync(string codigoUsuario, bool sucesso, string? detalhes = null);
        Task RegistrarAcaoAsync(string codigoUsuario, string acao, string detalhes);

        // Métodos de consulta de logs
        Task<List<LogSistema>> BuscarLogsPorUsuarioAsync(string codigoUsuario, DateTime? inicio = null, DateTime? fim = null);
        Task<List<LogSistema>> BuscarLogsPorAcaoAsync(string acao, DateTime? inicio = null, DateTime? fim = null);
        Task<List<LogSistema>> BuscarLogsPorPeriodoAsync(DateTime inicio, DateTime fim);

        // Métodos de auditoria
        Task<List<LogSistema>> GetLogsAuditoriaAsync(int quantidade = 100);
        Task<Dictionary<string, int>> GetEstatisticasLogsAsync();
        Task<bool> LimparLogsAntigosAsync(int diasParaManter = 90);

        // Métodos utilitários
        Task<string> ObterIPAddressAsync();
        string FormatarDetalhesJson(object objeto);
    }
}