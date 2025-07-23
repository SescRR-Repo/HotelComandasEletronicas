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

        // Métodos de criação e alteração
        Task<bool> CriarRegistroAsync(RegistroHospede registro);
        Task<bool> AlterarRegistroAsync(RegistroHospede registro);
        Task<bool> FinalizarRegistroAsync(int id);
        Task<bool> RegistrarHospedeAsync(RegistroHospede registro);

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
        Task<List<RegistroHospede>> BuscarPorNomeAsync(string nome);
        Task<List<RegistroHospede>> BuscarPorTelefoneAsync(string telefone);
    }
}