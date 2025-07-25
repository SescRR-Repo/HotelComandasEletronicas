using HotelComandasEletronicas.Models;
using HotelComandasEletronicas.ViewModels;

namespace HotelComandasEletronicas.Services
{
    public interface IConsultaService
    {
        // Métodos de consulta pública
        Task<ConsultaResultadoViewModel?> ConsultarPorQuartoAsync(string numeroQuarto);
        Task<ConsultaResultadoViewModel?> ConsultarPorNomeETelefoneAsync(string nome, string telefone);
        
        // Métodos de validação
        Task<bool> ValidarAcessoConsultaAsync(string numeroQuarto);
        Task<bool> ValidarNomeTelefoneAsync(string nome, string telefone, string numeroQuarto);
        
        // Métodos de extrato
        Task<ExtratoConsultaViewModel> GerarExtratoCompletoAsync(int registroHospedeId);
        Task<List<LancamentoConsumo>> ObterConsumosAsync(int registroHospedeId);
        
        // Métodos de segurança
        string MascararTelefone(string telefone);
        bool ValidarFormatoTelefone(string telefone);
        Task<bool> RegistroEstaAtivoAsync(int registroHospedeId);
        
        // Métodos utilitários
        Task<Dictionary<string, object>> ObterEstatisticasConsultaAsync(int registroHospedeId);
        string GerarResumoConsulta(ConsultaResultadoViewModel resultado);
    }
}
