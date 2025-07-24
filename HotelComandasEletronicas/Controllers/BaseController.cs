using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.Controllers
{
    public class BaseController : Controller
    {
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
                var codigo = HttpContext.Session.GetString("CodigoUsuario");
                if (!string.IsNullOrWhiteSpace(codigo))
                    return codigo;

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

        #region Métodos de Log Simplificados (APENAS SERILOG)

        /// <summary>
        /// Log estruturado usando apenas Serilog - OTIMIZADO
        /// </summary>
        // Método otimizado que usa apenas Serilog
        protected void LogarAcao(string acao, string detalhes = "", string tabela = "SISTEMA", int? registroId = null)
        {
            try
            {
                var logger = HttpContext.RequestServices.GetService<ILogger<BaseController>>();
                var usuario = UsuarioLogado?.Login ?? CodigoUsuarioAtual ?? "Sistema";
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

                // LOG ESTRUTURADO COMPLETO EM ARQUIVO
                logger?.LogInformation("🎯 AÇÃO: {Acao} | 👤 Usuário: {Usuario} | 📋 Tabela: {Tabela} | 🆔 ID: {RegistroId} | 📝 Detalhes: {Detalhes} | 🌐 IP: {IP}",
                    acao, usuario, tabela, registroId, detalhes, ip);
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetService<ILogger<BaseController>>();
                logger?.LogError(ex, "❌ Erro ao registrar log para ação: {Acao}", acao);
            }
        }

        /// <summary>
        /// Log específico para login/logout
        /// </summary>
        protected void LogarLogin(string usuario, bool sucesso, string detalhes = "")
        {
            try
            {
                var logger = HttpContext.RequestServices.GetService<ILogger<BaseController>>();
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                
                if (sucesso)
                {
                    logger?.LogInformation("✅ LOGIN SUCESSO: {Usuario} | 🌐 IP: {IP} | 📝 Detalhes: {Detalhes}", 
                        usuario, ip, detalhes);
                }
                else
                {
                    logger?.LogWarning("❌ LOGIN FALHOU: {Usuario} | 🌐 IP: {IP} | 📝 Detalhes: {Detalhes}", 
                        usuario, ip, detalhes);
                }
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetService<ILogger<BaseController>>();
                logger?.LogError(ex, "❌ Erro ao registrar log de login");
            }
        }

        #endregion

        #region Override de Métodos

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Passar dados do usuário para views
            ViewBag.UsuarioLogado = UsuarioLogado;
            ViewBag.CodigoUsuarioAtual = CodigoUsuarioAtual;
            ViewBag.UsuarioEstaLogado = UsuarioEstaLogado;
            ViewBag.UsuarioEhSupervisor = UsuarioEhSupervisor;
            ViewBag.UsuarioEhRecepcaoOuSupervisor = UsuarioEhRecepcaoOuSupervisor;

            base.OnActionExecuting(context);
        }

        #endregion
    }

    #region Attributes de Autorização

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