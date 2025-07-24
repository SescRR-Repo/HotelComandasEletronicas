using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using HotelComandasEletronicas.Models;
using HotelComandasEletronicas.Services;

namespace HotelComandasEletronicas.Controllers
{
    public class BaseController : Controller
    {
        protected ILogService? _logService;

        #region Propriedades de Sessão

        protected Usuario? UsuarioLogado
        {
            get
            {
                var loginUsuario = HttpContext.Session.GetString("UsuarioLogado");
                var nomeUsuario = HttpContext.Session.GetString("NomeUsuario");
                var perfilUsuario = HttpContext.Session.GetString("PerfilUsuario");
                var codigoUsuario = HttpContext.Session.GetString("CodigoUsuario");
                var usuarioId = HttpContext.Session.GetInt32("UsuarioID");

                if (string.IsNullOrWhiteSpace(loginUsuario) || !usuarioId.HasValue)
                    return null;

                return new Usuario
                {
                    ID = usuarioId.Value,
                    Login = loginUsuario,
                    Nome = nomeUsuario ?? "",
                    Perfil = perfilUsuario ?? "",
                    CodigoID = codigoUsuario ?? ""
                };
            }
        }

        protected string? CodigoUsuarioAtual
        {
            get
            {
                // Primeiro tenta sessão completa (login)
                var codigo = HttpContext.Session.GetString("CodigoUsuario");
                if (!string.IsNullOrWhiteSpace(codigo))
                    return codigo;

                // Depois tenta sessão temporária (validação de código)
                return HttpContext.Session.GetString("CodigoValidado");
            }
        }

        public bool UsuarioEstaLogado => UsuarioLogado != null;

        public bool UsuarioEhSupervisor => UsuarioLogado?.IsSupervisor() ?? false;

        public bool UsuarioEhRecepcaoOuSupervisor
        {
            get
            {
                var usuario = UsuarioLogado;
                return (usuario?.IsRecepcao() ?? false) || (usuario?.IsSupervisor() ?? false);
            }
        }

        #endregion

        #region Métodos de Verificação de Permissão

        protected bool VerificarPermissaoSupervisor()
        {
            if (!UsuarioEhSupervisor)
            {
                TempData["Erro"] = "Acesso negado. Apenas supervisores podem acessar esta funcionalidade.";
                return false;
            }
            return true;
        }

        protected bool VerificarPermissaoRecepcaoOuSupervisor()
        {
            if (!UsuarioEhRecepcaoOuSupervisor)
            {
                TempData["Erro"] = "Acesso negado. Apenas recepção e supervisores podem acessar esta funcionalidade.";
                return false;
            }
            return true;
        }

        protected bool VerificarLogin()
        {
            if (!UsuarioEstaLogado)
            {
                TempData["Aviso"] = "Você precisa fazer login para acessar esta página.";
                return false;
            }
            return true;
        }

        public bool VerificarCodigoOuLogin()
        {
            var temLogin = UsuarioEstaLogado;
            var temCodigo = !string.IsNullOrWhiteSpace(CodigoUsuarioAtual);

            if (!temLogin && !temCodigo)
            {
                TempData["Aviso"] = "Você precisa fazer login ou validar seu código para acessar esta página.";
                return false;
            }
            return true;
        }

        #endregion

        #region Métodos de Redirecionamento

        public IActionResult RedirecionarParaLogin(string? returnUrl = null)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl))
                return RedirectToAction("Login", "Usuario", new { returnUrl });

            return RedirectToAction("Login", "Usuario");
        }

        public IActionResult RedirecionarParaValidarCodigo(string? returnUrl = null)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl))
                return RedirectToAction("ValidarCodigo", "Usuario", new { returnUrl });

            return RedirectToAction("ValidarCodigo", "Usuario");
        }

        public IActionResult RedirecionarParaHome()
        {
            return RedirectToAction("Index", "Home");
        }

        protected IActionResult RedirecionarComSucesso(string controller, string action, string mensagem)
        {
            TempData["Sucesso"] = mensagem;
            return RedirectToAction(action, controller);
        }

        protected IActionResult RedirecionarComErro(string controller, string action, string mensagem)
        {
            TempData["Erro"] = mensagem;
            return RedirectToAction(action, controller);
        }

        #endregion

        #region Métodos de Sessão

        protected void CriarSessaoCompleta(Usuario usuario)
        {
            HttpContext.Session.SetString("UsuarioLogado", usuario.Login);
            HttpContext.Session.SetString("NomeUsuario", usuario.Nome);
            HttpContext.Session.SetString("PerfilUsuario", usuario.Perfil);
            HttpContext.Session.SetString("CodigoUsuario", usuario.CodigoID);
            HttpContext.Session.SetInt32("UsuarioID", usuario.ID);
        }

        protected void CriarSessaoTemporaria(Usuario usuario)
        {
            HttpContext.Session.SetString("CodigoValidado", usuario.CodigoID);
            HttpContext.Session.SetString("NomeUsuarioTemporario", usuario.Nome);
            HttpContext.Session.SetString("PerfilUsuarioTemporario", usuario.Perfil);
        }

        protected void LimparSessao()
        {
            HttpContext.Session.Clear();
        }

        protected void DefinirMensagemSucesso(string mensagem)
        {
            TempData["Sucesso"] = mensagem;
        }

        protected void DefinirMensagemErro(string mensagem)
        {
            TempData["Erro"] = mensagem;
        }

        protected void DefinirMensagemAviso(string mensagem)
        {
            TempData["Aviso"] = mensagem;
        }

        protected void DefinirMensagemInfo(string mensagem)
        {
            TempData["Info"] = mensagem;
        }

        #endregion

        #region Métodos de Log e Auditoria - MELHORADOS

        /// <summary>
        /// Loga ação na tabela LOGS_SISTEMA do banco de dados
        /// </summary>
        protected void LogarAcao(string acao, string detalhes = "", string tabela = "SISTEMA", int? registroId = null)
        {
            // Executar de forma assíncrona sem bloquear
            _ = Task.Run(async () =>
            {
                try
                {
                    // Obter LogService se não foi injetado ainda
                    _logService ??= HttpContext.RequestServices.GetService<ILogService>();

                    if (_logService == null) return;

                    var usuario = UsuarioLogado?.Login ?? CodigoUsuarioAtual ?? "Sistema";
                    var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

                    // Gravar no banco através do LogService
                    await _logService.RegistrarLogAsync(
                        codigoUsuario: usuario,
                        acao: acao,
                        tabela: tabela,
                        registroId: registroId,
                        detalhesAntes: null,
                        detalhesDepois: detalhes
                    );

                    // Também manter o log no Serilog para desenvolvimento
                    var logger = HttpContext.RequestServices.GetService<ILogger<BaseController>>();
                    logger?.LogInformation("Usuário {Usuario} executou ação: {Acao}. Detalhes: {Detalhes}. IP: {IP}",
                        usuario, acao, detalhes, ip);
                }
                catch (Exception ex)
                {
                    // Se falhar o log personalizado, pelo menos logar o erro
                    var logger = HttpContext.RequestServices.GetService<ILogger<BaseController>>();
                    logger?.LogError(ex, "Erro ao registrar log personalizado para ação: {Acao}", acao);
                }
            });
        }

        /// <summary>
        /// Loga ação de login específica
        /// </summary>
        protected void LogarLogin(string usuario, bool sucesso, string detalhes = "")
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    _logService ??= HttpContext.RequestServices.GetService<ILogService>();

                    if (_logService != null)
                    {
                        await _logService.RegistrarLoginAsync(usuario, sucesso, detalhes);
                    }
                }
                catch (Exception ex)
                {
                    var logger = HttpContext.RequestServices.GetService<ILogger<BaseController>>();
                    logger?.LogError(ex, "Erro ao registrar log de login");
                }
            });
        }

        /// <summary>
        /// Loga alteração em registro específico (para auditoria completa)
        /// </summary>
        protected void LogarAlteracao(string acao, string tabela, int registroId,
            object? estadoAnterior = null, object? estadoPosterior = null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    _logService ??= HttpContext.RequestServices.GetService<ILogService>();

                    if (_logService == null) return;

                    var usuario = UsuarioLogado?.Login ?? CodigoUsuarioAtual ?? "Sistema";

                    var detalhesAntes = estadoAnterior != null ?
                        _logService.FormatarDetalhesJson(estadoAnterior) : null;

                    var detalhesDepois = estadoPosterior != null ?
                        _logService.FormatarDetalhesJson(estadoPosterior) : null;

                    await _logService.RegistrarLogAsync(
                        codigoUsuario: usuario,
                        acao: acao,
                        tabela: tabela,
                        registroId: registroId,
                        detalhesAntes: detalhesAntes,
                        detalhesDepois: detalhesDepois
                    );
                }
                catch (Exception ex)
                {
                    var logger = HttpContext.RequestServices.GetService<ILogger<BaseController>>();
                    logger?.LogError(ex, "Erro ao registrar log de alteração");
                }
            });
        }

        #endregion

        #region Override de Métodos

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Obter o LogService para usar nos métodos de log
            _logService = HttpContext.RequestServices.GetService<ILogService>();

            // Passar dados do usuário para todas as views
            ViewBag.UsuarioLogado = UsuarioLogado;
            ViewBag.CodigoUsuarioAtual = CodigoUsuarioAtual;
            ViewBag.UsuarioEstaLogado = UsuarioEstaLogado;
            ViewBag.UsuarioEhSupervisor = UsuarioEhSupervisor;
            ViewBag.UsuarioEhRecepcaoOuSupervisor = UsuarioEhRecepcaoOuSupervisor;

            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            // Log automático de todas as ações (opcional)
            var actionName = context.ActionDescriptor.DisplayName ?? "Unknown";
            var controllerName = context.Controller.GetType().Name;

            // Só loga ações importantes, não todas
            if (ShouldLogAction(actionName))
            {
                LogarAcao($"Acao{controllerName}", $"Executou: {actionName}");
            }

            base.OnActionExecuted(context);
        }

        /// <summary>
        /// Define quais ações devem ser logadas automaticamente
        /// </summary>
        private bool ShouldLogAction(string actionName)
        {
            var actionsToLog = new[]
            {
                "Login", "Logout", "Cadastrar", "Editar", "Inativar", "Ativar",
                "Cancelar", "Finalizar", "Registrar", "Alterar"
            };

            return actionsToLog.Any(action => actionName.Contains(action, StringComparison.OrdinalIgnoreCase));
        }

        #endregion
    }

    #region Attributes de Autorização (mantidos iguais)

    public class RequireLoginAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = context.Controller as BaseController;
            if (controller != null && !controller.UsuarioEstaLogado)
            {
                controller.TempData["Aviso"] = "Você precisa fazer login para acessar esta página.";
                context.Result = controller.RedirecionarParaLogin(context.HttpContext.Request.Path);
            }
            base.OnActionExecuting(context);
        }
    }

    public class RequireSupervisorAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = context.Controller as BaseController;
            if (controller != null)
            {
                if (!controller.UsuarioEstaLogado)
                {
                    controller.TempData["Aviso"] = "Você precisa fazer login para acessar esta página.";
                    context.Result = controller.RedirecionarParaLogin(context.HttpContext.Request.Path);
                }
                else if (!controller.UsuarioEhSupervisor)
                {
                    controller.TempData["Erro"] = "Acesso negado. Apenas supervisores podem acessar esta funcionalidade.";
                    context.Result = controller.RedirecionarParaHome();
                }
            }
            base.OnActionExecuting(context);
        }
    }

    public class RequireRecepcaoOuSupervisorAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = context.Controller as BaseController;
            if (controller != null)
            {
                if (!controller.UsuarioEstaLogado)
                {
                    controller.TempData["Aviso"] = "Você precisa fazer login para acessar esta página.";
                    context.Result = controller.RedirecionarParaLogin(context.HttpContext.Request.Path);
                }
                else if (!controller.UsuarioEhRecepcaoOuSupervisor)
                {
                    controller.TempData["Erro"] = "Acesso negado. Apenas recepção e supervisores podem acessar esta funcionalidade.";
                    context.Result = controller.RedirecionarParaHome();
                }
            }
            base.OnActionExecuting(context);
        }
    }

    public class RequireCodigoOuLoginAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = context.Controller as BaseController;
            if (controller != null && !controller.VerificarCodigoOuLogin())
            {
                context.Result = controller.RedirecionarParaValidarCodigo(context.HttpContext.Request.Path);
            }
            base.OnActionExecuting(context);
        }
    }

    #endregion
}