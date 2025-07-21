using Microsoft.AspNetCore.Mvc;
using HotelComandasEletronicas.Services;

namespace HotelComandasEletronicas.Controllers
{
    [Route("api/usuario")]
    [ApiController]
    public class ApiUsuarioController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;

        public ApiUsuarioController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        /// <summary>
        /// Validar código de usuário via AJAX
        /// </summary>
        [HttpPost("validar-codigo")]
        public async Task<IActionResult> ValidarCodigo([FromBody] ValidarCodigoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Codigo) || request.Codigo.Length != 2)
            {
                return BadRequest(new { sucesso = false, mensagem = "Código deve ter exatamente 2 dígitos" });
            }

            var usuario = await _usuarioService.ValidarCodigoAsync(request.Codigo);

            if (usuario == null)
            {
                return Ok(new
                {
                    sucesso = false,
                    mensagem = "Código inválido ou usuário inativo",
                    codigo = request.Codigo
                });
            }

            return Ok(new
            {
                sucesso = true,
                mensagem = $"Código validado para {usuario.Nome}",
                usuario = new
                {
                    id = usuario.ID,
                    nome = usuario.Nome,
                    perfil = usuario.Perfil,
                    codigo = usuario.CodigoID
                }
            });
        }

        /// <summary>
        /// Verificar se login já existe
        /// </summary>
        [HttpGet("verificar-login/{login}")]
        public async Task<IActionResult> VerificarLogin(string login)
        {
            var existe = await _usuarioService.LoginJaExisteAsync(login);
            return Ok(new { existe = existe });
        }

        /// <summary>
        /// Verificar se código já existe
        /// </summary>
        [HttpGet("verificar-codigo/{codigo}")]
        public async Task<IActionResult> VerificarCodigo(string codigo)
        {
            var existe = await _usuarioService.CodigoJaExisteAsync(codigo);
            return Ok(new { existe = existe });
        }

        /// <summary>
        /// Gerar código único
        /// </summary>
        [HttpGet("gerar-codigo")]
        public IActionResult GerarCodigo()
        {
            var codigo = _usuarioService.GerarCodigoUnico();
            return Ok(new { codigo = codigo });
        }

        /// <summary>
        /// Obter estatísticas dos usuários
        /// </summary>
        [HttpGet("estatisticas")]
        public async Task<IActionResult> ObterEstatisticas()
        {
            var usuarios = await _usuarioService.ListarAtivosAsync();

            var estatisticas = new
            {
                total = usuarios.Count,
                garcons = usuarios.Count(u => u.IsGarcom()),
                recepcao = usuarios.Count(u => u.IsRecepcao()),
                supervisores = usuarios.Count(u => u.IsSupervisor()),
                ativos = usuarios.Count(u => u.Status),
                ultimoLogin = usuarios
                    .Where(u => u.UltimoAcesso.HasValue)
                    .OrderByDescending(u => u.UltimoAcesso)
                    .FirstOrDefault()?.UltimoAcesso
            };

            return Ok(estatisticas);
        }
    }

    /// <summary>
    /// Modelo para requisição de validação de código
    /// </summary>
    public class ValidarCodigoRequest
    {
        public string Codigo { get; set; } = string.Empty;
    }
}