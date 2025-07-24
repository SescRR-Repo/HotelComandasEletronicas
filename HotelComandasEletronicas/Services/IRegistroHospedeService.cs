using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.Services
{
    public interface IRegistroHospedeService
    {
        // Métodos de CRUD básico
        Task<RegistroHospede?> BuscarPorIdAsync(int id);
        Task<RegistroHospede?> BuscarPorQuartoAsync(string numeroQuarto);
        Task<List<RegistroHospede>> ListarTodosAsync();
        Task<List<RegistroHospede>> ListarAtivosAsync();
        Task<List<RegistroHospede>> ListarFinalizadosAsync();

        // Métodos de criação e alteração
        Task<bool> CriarRegistroAsync(RegistroHospede registro);
        Task<bool> AlterarRegistroAsync(RegistroHospede registro);
        Task<bool> FinalizarRegistroAsync(int id);
        Task<bool> FinalizarRegistroAsync(int id, string usuarioFinalizacao);
        Task<bool> RegistrarHospedeAsync(RegistroHospede registro);
        Task<bool> AlterarHospedeAsync(RegistroHospede registro);

        // Métodos de busca inteligente
        Task<List<RegistroHospede>> BuscarPorTextoAsync(string termo);
        Task<List<RegistroHospede>> BuscarPorNomeAsync(string nome);
        Task<List<RegistroHospede>> BuscarPorTelefoneAsync(string telefone);
        Task<RegistroHospede?> BuscarPorNomeETelefoneAsync(string nome, string telefone);
        Task<List<RegistroHospede>> BuscarComFiltrosAsync(string? quarto = null, string? nome = null,
            string? status = null, DateTime? inicio = null, DateTime? fim = null);
        Task<List<RegistroHospede>> ListarPorPeriodoAsync(DateTime inicio, DateTime fim);
        string DetectarTipoBusca(string termo);

        // Métodos de validação
        Task<bool> QuartoJaExisteAsync(string numeroQuarto, int? excluirId = null);
        Task<bool> PodeFinalizarAsync(int id);
        Task<bool> TemConsumosAtivosAsync(int id);

        // Métodos de cálculo
        Task AtualizarValorGastoAsync(int hospedeId);
        Task<decimal> CalcularTotalGeralAsync();
        Task<int> ContarHospedesAtivosAsync();

        // Métodos utilitários
        Task<Dictionary<string, object>> ObterEstatisticasAsync();
    }
}