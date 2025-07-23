using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.Services
{
    public interface ILancamentoService
    {
        // Métodos de CRUD
        Task<bool> RegistrarConsumoAsync(LancamentoConsumo lancamento);
        Task<bool> CancelarLancamentoAsync(int id, string motivo, string usuarioCancelamento);
        Task<bool> RemoverItemCarrinhoAsync(int id);
        Task<LancamentoConsumo?> BuscarPorIdAsync(int id);

        // Métodos de busca
        Task<List<LancamentoConsumo>> GetConsumosPorHospedeAsync(int hospedeId);
        Task<List<LancamentoConsumo>> GetConsumosPorPeriodoAsync(DateTime inicio, DateTime fim);
        Task<List<LancamentoConsumo>> GetConsumosPorUsuarioAsync(string codigoUsuario);
        Task<List<LancamentoConsumo>> ListarTodosAsync();

        // Métodos de cálculo
        Task<decimal> CalcularTotalPeriodoAsync(DateTime inicio, DateTime fim);
        Task<decimal> CalcularTotalHospedeAsync(int hospedeId);

        // Métodos de validação
        Task<bool> ValidarPermissaoCancelamentoAsync(string codigoUsuario);
        Task<bool> PodeCancelarAsync(int lancamentoId, string codigoUsuario);

        // Métodos utilitários
        Task<Dictionary<string, object>> ObterEstatisticasAsync();
    }
}