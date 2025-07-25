using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.Services
{
    public interface IConsultaClienteService
    {
        // Métodos de validação de cliente
        Task<RegistroHospede?> ValidarClienteAsync(string quarto, string? nome = null, string? telefone = null);
        Task<RegistroHospede?> BuscarPorQuartoAsync(string numeroQuarto);
        Task<RegistroHospede?> BuscarPorNomeETelefoneAsync(string nome, string telefone);

        // Métodos de extrato
        Task<List<LancamentoConsumo>> GetExtratoPorQuartoAsync(string numeroQuarto);
        Task<List<LancamentoConsumo>> GetExtratoPorHospedeAsync(int hospedeId);
        Task<ExtratoCliente> GerarExtratoClienteAsync(int hospedeId);

        // Métodos de validação
        Task<bool> ValidarDadosClienteAsync(string? quarto = null, string? nome = null, string? telefone = null);
    }
}