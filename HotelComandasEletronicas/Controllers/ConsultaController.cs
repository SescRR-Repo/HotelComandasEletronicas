using Microsoft.AspNetCore.Mvc;
using HotelComandasEletronicas.Services;
using HotelComandasEletronicas.ViewModels;

namespace HotelComandasEletronicas.Controllers
{
    public class ConsultaController : Controller
    {
        private readonly IConsultaService _consultaService;
        private readonly ILogger<ConsultaController> _logger;

        public ConsultaController(IConsultaService consultaService, ILogger<ConsultaController> logger)
        {
            _consultaService = consultaService;
            _logger = logger;
        }

        #region Views Principais

        /// <summary>
        /// Página principal de consulta pública (sem autenticação)
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            _logger.LogInformation("Acesso à página de consulta pública - IP: {IP}", 
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");

            var viewModel = new ConsultaViewModel();
            return View(viewModel);
        }

        /// <summary>
        /// Processar consulta por quarto ou nome+telefone
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ConsultaViewModel model)
        {
            try
            {
                model.ConsultaRealizada = true;
                model.ResultadoEncontrado = false;
                model.MensagemErro = null;

                // Validar dados de entrada
                if (model.IsConsultaPorQuarto && !model.TemDadosConsultaQuarto)
                {
                    model.MensagemErro = "Digite o número do quarto para consulta.";
                    return View(model);
                }

                if (model.IsConsultaPorNomeTelefone && !model.TemDadosConsultaNomeTelefone)
                {
                    model.MensagemErro = "Digite o nome e telefone para consulta.";
                    return View(model);
                }

                // Realizar consulta baseada no método escolhido
                ConsultaResultadoViewModel? resultado = null;

                if (model.IsConsultaPorQuarto)
                {
                    resultado = await _consultaService.ConsultarPorQuartoAsync(model.NumeroQuarto!);
                    _logger.LogInformation("Consulta por quarto realizada: {Quarto}", model.NumeroQuarto);
                }
                else if (model.IsConsultaPorNomeTelefone)
                {
                    resultado = await _consultaService.ConsultarPorNomeETelefoneAsync(model.Nome!, model.Telefone!);
                    _logger.LogInformation("Consulta por nome e telefone realizada: {Nome}", model.Nome);
                }

                if (resultado != null)
                {
                    model.Resultado = resultado;
                    model.ResultadoEncontrado = true;
                    
                    // Log de sucesso sem dados sensíveis
                    _logger.LogInformation("Consulta bem-sucedida - Quarto: {Quarto}, Método: {Metodo}", 
                        resultado.NumeroQuarto, model.MetodoConsulta);
                }
                else
                {
                    model.MensagemErro = model.IsConsultaPorQuarto ? 
                        "Quarto não encontrado ou não possui hóspede ativo." :
                        "Dados não conferem. Verifique o nome e telefone informados.";
                    
                    _logger.LogWarning("Consulta sem resultado - Método: {Metodo}", model.MetodoConsulta);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar consulta");
                model.MensagemErro = "Erro interno do sistema. Tente novamente em alguns minutos.";
                return View(model);
            }
        }

        /// <summary>
        /// Exibir extrato completo de consumos
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Extrato(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Tentativa de acesso ao extrato com ID inválido: {Id}", id);
                    return RedirectToAction("Index");
                }

                // Verificar se o registro ainda está ativo
                if (!await _consultaService.RegistroEstaAtivoAsync(id))
                {
                    _logger.LogWarning("Tentativa de acesso ao extrato de registro inativo: {Id}", id);
                    TempData["Erro"] = "Este registro não está mais ativo.";
                    return RedirectToAction("Index");
                }

                var extrato = await _consultaService.GerarExtratoCompletoAsync(id);
                
                if (extrato.RegistroId == 0)
                {
                    _logger.LogWarning("Extrato não encontrado para ID: {Id}", id);
                    TempData["Erro"] = "Extrato não encontrado.";
                    return RedirectToAction("Index");
                }

                _logger.LogInformation("Extrato acessado - Quarto: {Quarto}, ID: {Id}", 
                    extrato.NumeroQuarto, id);

                return View(extrato);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar extrato para ID: {Id}", id);
                TempData["Erro"] = "Erro ao gerar extrato. Tente novamente.";
                return RedirectToAction("Index");
            }
        }

        #endregion

        #region APIs AJAX (Opcionais)

        /// <summary>
        /// Validar se quarto existe (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ValidarQuarto(string numero)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(numero))
                    return Json(new { existe = false });

                var existe = await _consultaService.ValidarAcessoConsultaAsync(numero);
                return Json(new { existe = existe });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar quarto: {Numero}", numero);
                return Json(new { existe = false });
            }
        }

        /// <summary>
        /// Obter estatísticas rápidas (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EstatisticasRapidas(int registroId)
        {
            try
            {
                var stats = await _consultaService.ObterEstatisticasConsultaAsync(registroId);
                return Json(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter estatísticas: {RegistroId}", registroId);
                return Json(new { erro = "Erro ao carregar estatísticas" });
            }
        }

        #endregion

        #region Métodos Utilitários

        /// <summary>
        /// Página de ajuda sobre como usar o sistema de consulta
        /// </summary>
        [HttpGet]
        public IActionResult Ajuda()
        {
            return View();
        }

        /// <summary>
        /// Página de políticas de privacidade
        /// </summary>
        [HttpGet]
        public IActionResult Privacidade()
        {
            return View();
        }

        #endregion
    }
}
