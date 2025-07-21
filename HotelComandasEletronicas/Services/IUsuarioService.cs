using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.Services
{
    public interface IUsuarioService
    {
        // Métodos de autenticação
        Task<Usuario?> ValidarLoginAsync(string login, string senha);
        Task<Usuario?> ValidarCodigoAsync(string codigoID);

        // Métodos de CRUD
        Task<bool> CadastrarUsuarioAsync(Usuario usuario);
        Task<bool> AlterarUsuarioAsync(Usuario usuario);
        Task<bool> InativarUsuarioAsync(int id);
        Task<Usuario?> BuscarPorIdAsync(int id);
        Task<Usuario?> BuscarPorLoginAsync(string login);
        Task<Usuario?> BuscarPorCodigoAsync(string codigoID);

        // Métodos de consulta
        Task<List<Usuario>> ListarTodosAsync();
        Task<List<Usuario>> ListarPorPerfilAsync(string perfil);
        Task<List<Usuario>> ListarAtivosAsync();

        // Métodos de validação
        Task<bool> LoginJaExisteAsync(string login);
        Task<bool> CodigoJaExisteAsync(string codigoID);
        Task<bool> ValidarPermissaoAsync(string codigoID, string operacao);

        // Métodos utilitários
        string GerarCodigoUnico();
        string CriptografarSenha(string senha);
        bool VerificarSenha(string senha, string senhaHash);
        Task AtualizarUltimoAcessoAsync(int usuarioId);
    }
}