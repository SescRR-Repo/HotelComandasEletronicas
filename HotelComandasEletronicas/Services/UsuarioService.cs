using Microsoft.EntityFrameworkCore;
using HotelComandasEletronicas.Data;
using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly ComandasDbContext _context;
        private readonly ILogger<UsuarioService> _logger;

        public UsuarioService(ComandasDbContext context, ILogger<UsuarioService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Métodos de Autenticação

        public async Task<Usuario?> ValidarLoginAsync(string login, string senha)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(senha))
                    return null;

                var usuario = await _context.Usuarios
                    .Where(u => u.Login == login && u.Status)
                    .FirstOrDefaultAsync();

                if (usuario == null)
                {
                    _logger.LogWarning("Tentativa de login com usuário inexistente: {Login}", login);
                    return null;
                }

                // Verificar se usuário tem permissão para login (apenas Recepção e Supervisor)
                if (!usuario.TemPermissaoLogin())
                {
                    _logger.LogWarning("Usuário {Login} sem permissão para login (Perfil: {Perfil})", login, usuario.Perfil);
                    return null;
                }

                // Verificar senha
                if (!VerificarSenha(senha, usuario.Senha))
                {
                    _logger.LogWarning("Senha incorreta para usuário: {Login}", login);
                    return null;
                }

                // Atualizar último acesso
                await AtualizarUltimoAcessoAsync(usuario.ID);

                _logger.LogInformation("Login bem-sucedido para usuário: {Login} ({Perfil})", login, usuario.Perfil);
                return usuario;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar login para usuário: {Login}", login);
                return null;
            }
        }

        public async Task<Usuario?> ValidarCodigoAsync(string codigoID)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigoID) || codigoID.Length != 2)
                    return null;

                var usuario = await _context.Usuarios
                    .Where(u => u.CodigoID == codigoID && u.Status)
                    .FirstOrDefaultAsync();

                if (usuario != null)
                {
                    _logger.LogInformation("Código {CodigoID} validado para usuário: {Nome} ({Perfil})",
                        codigoID, usuario.Nome, usuario.Perfil);
                }
                else
                {
                    _logger.LogWarning("Tentativa de uso de código inexistente: {CodigoID}", codigoID);
                }

                return usuario;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar código: {CodigoID}", codigoID);
                return null;
            }
        }

        #endregion

        #region Métodos de CRUD

        public async Task<bool> CadastrarUsuarioAsync(Usuario usuario)
        {
            try
            {
                // Validações
                if (await LoginJaExisteAsync(usuario.Login))
                {
                    _logger.LogWarning("Tentativa de cadastrar usuário com login já existente: {Login}", usuario.Login);
                    return false;
                }

                if (await CodigoJaExisteAsync(usuario.CodigoID))
                {
                    _logger.LogWarning("Tentativa de cadastrar usuário com código já existente: {CodigoID}", usuario.CodigoID);
                    return false;
                }

                // Criptografar senha (apenas para Recepção e Supervisor)
                if (usuario.TemPermissaoLogin() && !string.IsNullOrWhiteSpace(usuario.Senha))
                {
                    usuario.Senha = CriptografarSenha(usuario.Senha);
                }

                usuario.DataCadastro = DateTime.Now;
                usuario.Status = true;

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuário cadastrado com sucesso: {Nome} ({Login}, {CodigoID})",
                    usuario.Nome, usuario.Login, usuario.CodigoID);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cadastrar usuário: {Nome}", usuario.Nome);
                return false;
            }
        }

        public async Task<bool> AlterarUsuarioAsync(Usuario usuario)
        {
            try
            {
                var usuarioExistente = await _context.Usuarios.FindAsync(usuario.ID);
                if (usuarioExistente == null)
                    return false;

                // Verificar se login já existe (exceto o próprio usuário)
                var loginExiste = await _context.Usuarios
                    .AnyAsync(u => u.Login == usuario.Login && u.ID != usuario.ID);
                if (loginExiste)
                    return false;

                // Verificar se código já existe (exceto o próprio usuário)
                var codigoExiste = await _context.Usuarios
                    .AnyAsync(u => u.CodigoID == usuario.CodigoID && u.ID != usuario.ID);
                if (codigoExiste)
                    return false;

                // Atualizar campos
                usuarioExistente.Nome = usuario.Nome;
                usuarioExistente.Login = usuario.Login;
                usuarioExistente.CodigoID = usuario.CodigoID;
                usuarioExistente.Perfil = usuario.Perfil;

                // Atualizar senha apenas se fornecida
                if (!string.IsNullOrWhiteSpace(usuario.Senha))
                {
                    usuarioExistente.Senha = CriptografarSenha(usuario.Senha);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuário alterado com sucesso: {Nome} (ID: {ID})", usuario.Nome, usuario.ID);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao alterar usuário: {ID}", usuario.ID);
                return false;
            }
        }

        public async Task<bool> InativarUsuarioAsync(int id)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(id);
                if (usuario == null)
                    return false;

                usuario.Status = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuário inativado: {Nome} (ID: {ID})", usuario.Nome, id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inativar usuário: {ID}", id);
                return false;
            }
        }

        #endregion

        #region Métodos de Busca

        public async Task<Usuario?> BuscarPorIdAsync(int id)
        {
            try
            {
                return await _context.Usuarios.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar usuário por ID: {ID}", id);
                return null;
            }
        }

        public async Task<Usuario?> BuscarPorLoginAsync(string login)
        {
            try
            {
                return await _context.Usuarios
                    .Where(u => u.Login == login)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar usuário por login: {Login}", login);
                return null;
            }
        }

        public async Task<Usuario?> BuscarPorCodigoAsync(string codigoID)
        {
            try
            {
                return await _context.Usuarios
                    .Where(u => u.CodigoID == codigoID)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar usuário por código: {CodigoID}", codigoID);
                return null;
            }
        }

        public async Task<List<Usuario>> ListarTodosAsync()
        {
            try
            {
                return await _context.Usuarios
                    .OrderBy(u => u.Nome)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar todos os usuários");
                return new List<Usuario>();
            }
        }

        public async Task<List<Usuario>> ListarPorPerfilAsync(string perfil)
        {
            try
            {
                return await _context.Usuarios
                    .Where(u => u.Perfil == perfil && u.Status)
                    .OrderBy(u => u.Nome)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar usuários por perfil: {Perfil}", perfil);
                return new List<Usuario>();
            }
        }

        public async Task<List<Usuario>> ListarAtivosAsync()
        {
            try
            {
                return await _context.Usuarios
                    .Where(u => u.Status)
                    .OrderBy(u => u.Nome)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar usuários ativos");
                return new List<Usuario>();
            }
        }

        #endregion

        #region Métodos de Validação

        public async Task<bool> LoginJaExisteAsync(string login)
        {
            try
            {
                return await _context.Usuarios.AnyAsync(u => u.Login == login);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar se login já existe: {Login}", login);
                return true; // Retornar true em caso de erro para evitar duplicatas
            }
        }

        public async Task<bool> CodigoJaExisteAsync(string codigoID)
        {
            try
            {
                return await _context.Usuarios.AnyAsync(u => u.CodigoID == codigoID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar se código já existe: {CodigoID}", codigoID);
                return true; // Retornar true em caso de erro para evitar duplicatas
            }
        }

        public async Task<bool> ValidarPermissaoAsync(string codigoID, string operacao)
        {
            try
            {
                var usuario = await ValidarCodigoAsync(codigoID);
                if (usuario == null)
                    return false;

                return operacao.ToLower() switch
                {
                    "lancamento" => true, // Todos podem lançar consumos
                    "cancelamento" => usuario.TemPermissaoCancelamento(), // Apenas Recepção e Supervisor
                    "cadastro" => usuario.TemPermissaoCadastro(), // Apenas Supervisor
                    "relatorio" => usuario.TemPermissaoLogin(), // Recepção e Supervisor
                    _ => false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar permissão: {CodigoID} para {Operacao}", codigoID, operacao);
                return false;
            }
        }

        #endregion

        #region Métodos Utilitários

        public string GerarCodigoUnico()
        {
            var random = new Random();
            string codigo;

            do
            {
                // Gerar código de 2 dígitos (10-99)
                codigo = random.Next(10, 100).ToString();
            }
            while (_context.Usuarios.Any(u => u.CodigoID == codigo));

            return codigo;
        }

        public string CriptografarSenha(string senha)
        {
            return BCrypt.Net.BCrypt.HashPassword(senha);
        }

        public bool VerificarSenha(string senha, string senhaHash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(senha, senhaHash);
            }
            catch
            {
                return false;
            }
        }

        public async Task AtualizarUltimoAcessoAsync(int usuarioId)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(usuarioId);
                if (usuario != null)
                {
                    usuario.UltimoAcesso = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar último acesso: {UsuarioId}", usuarioId);
            }
        }

        #endregion
    }
}