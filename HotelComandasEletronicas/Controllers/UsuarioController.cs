using Microsoft.AspNetCore.Mvc;
using HotelComandasEletronicas.Services;
using HotelComandasEletronicas.ViewModels;
using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.Controllers
{
    public class UsuarioController : BaseController
    {
        private readonly IUsuarioService _usuarioService;
        private readonly ILogger<UsuarioController> _logger;
        
        public UsuarioController(IUsuarioService usuarioService, ILogger<UsuarioController> logger)
        {
            _usuarioService = usuarioService;
            _logger = logger;
        }

        #region Autenticação

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Se já estiver logado, redirecionar
            if (UsuarioEstaLogado)
            {
                return RedirecionarParaHome();
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = await _usuarioService.ValidarLoginAsync(model.Login, model.Senha);

            if (usuario == null)
            {
                model.MensagemErro = "Login ou senha incorretos, ou usuário sem permissão de acesso.";
                ModelState.AddModelError("", model.MensagemErro);
                return View(model);
            }

            // Criar sessão do usuário
            CriarSessaoCompleta(usuario);

            DefinirMensagemSucesso($"Bem-vindo(a), {usuario.Nome}!");
            LogarAcao("Login", $"Usuário {usuario.Login} ({usuario.Perfil}) fez login com sucesso");

            // Redirecionar para URL de retorno ou dashboard
            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirecionarParaHome();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            var nomeUsuario = HttpContext.Session.GetString("NomeUsuario");

            // Limpar sessão
            LimparSessao();

            DefinirMensagemInfo($"Logout realizado com sucesso. Até logo, {nomeUsuario}!");
            LogarAcao("Logout", $"Usuário {nomeUsuario} fez logout");

            return RedirecionarParaHome();
        }

        [HttpGet]
        public IActionResult ValidarCodigo(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new ValidarCodigoViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidarCodigo(ValidarCodigoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = await _usuarioService.ValidarCodigoAsync(model.Codigo);

            if (usuario == null)
            {
                model.MensagemErro = "Código inválido ou usuário inativo.";
                ModelState.AddModelError("", model.MensagemErro);
                return View(model);
            }

            // Para garçom, criar sessão temporária apenas com código
            CriarSessaoTemporaria(usuario);

            DefinirMensagemSucesso($"Código validado! Usuário: {usuario.Nome} ({usuario.Perfil})");
            LogarAcao("ValidarCodigo", $"Código {model.Codigo} validado para {usuario.Nome}");

            // Redirecionar baseado no perfil
            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            // Garçom vai direto para lançamento
            if (usuario.IsGarcom())
            {
                return RedirectToAction("Index", "Lancamento");
            }

            return RedirecionarParaHome();
        }

        #endregion

        #region Gestão de Usuários (Apenas Supervisor)

        [HttpGet]
        [RequireSupervisor]
        public async Task<IActionResult> Index()
        {
            var usuarios = await _usuarioService.ListarTodosAsync();
            var viewModels = usuarios.Select(UsuarioViewModel.DeModel).ToList();

            return View(viewModels);
        }

        [HttpGet]
        [RequireSupervisor]
        public IActionResult Cadastrar()
        {
            ViewBag.PerfisDisponiveis = UsuarioViewModel.PerfisDisponiveis;
            return View(new UsuarioViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireSupervisor]
        public async Task<IActionResult> Cadastrar(UsuarioViewModel model)
        {
            ViewBag.PerfisDisponiveis = UsuarioViewModel.PerfisDisponiveis;

            // Validações customizadas
            if (!model.IsValido(out var erros))
            {
                foreach (var erro in erros)
                {
                    ModelState.AddModelError("", erro);
                }
                return View(model);
            }

            // Verificar se login já existe
            if (await _usuarioService.LoginJaExisteAsync(model.Login))
            {
                ModelState.AddModelError("Login", "Este login já está em uso.");
                return View(model);
            }

            // Verificar se código já existe
            if (await _usuarioService.CodigoJaExisteAsync(model.CodigoID))
            {
                ModelState.AddModelError("CodigoID", "Este código já está em uso.");
                return View(model);
            }

            var usuario = model.ParaModel();
            var sucesso = await _usuarioService.CadastrarUsuarioAsync(usuario);

            if (sucesso)
            {
                return RedirecionarComSucesso("Usuario", "Index", $"Usuário {model.Nome} cadastrado com sucesso!");
            }
            else
            {
                DefinirMensagemErro("Erro ao cadastrar usuário. Tente novamente.");
                return View(model);
            }
        }

        [HttpGet]
        [RequireSupervisor]
        public async Task<IActionResult> Editar(int id)
        {
            var usuario = await _usuarioService.BuscarPorIdAsync(id);
            if (usuario == null)
            {
                return RedirecionarComErro("Usuario", "Index", "Usuário não encontrado.");
            }

            ViewBag.PerfisDisponiveis = UsuarioViewModel.PerfisDisponiveis;
            return View(UsuarioViewModel.DeModel(usuario));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireSupervisor]
        public async Task<IActionResult> Editar(UsuarioViewModel model)
        {
            ViewBag.PerfisDisponiveis = UsuarioViewModel.PerfisDisponiveis;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = model.ParaModel();
            var sucesso = await _usuarioService.AlterarUsuarioAsync(usuario);

            if (sucesso)
            {
                return RedirecionarComSucesso("Usuario", "Index", $"Usuário {model.Nome} alterado com sucesso!");
            }
            else
            {
                DefinirMensagemErro("Erro ao alterar usuário. Verifique se login e código não estão em uso.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireSupervisor]
        public async Task<IActionResult> Inativar(int id)
        {
            var sucesso = await _usuarioService.InativarUsuarioAsync(id);

            if (sucesso)
            {
                return Json(new { sucesso = true, mensagem = "Usuário inativado com sucesso." });
            }
            else
            {
                return Json(new { sucesso = false, mensagem = "Erro ao inativar usuário." });
            }
        }

        [HttpGet]
        [RequireSupervisor]
        public IActionResult GerarCodigo()
        {
            var codigo = _usuarioService.GerarCodigoUnico();
            return Json(new { codigo = codigo });
        }

        #endregion
    }
}