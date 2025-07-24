using Microsoft.AspNetCore.Mvc;
using HotelComandasEletronicas.Services;
using HotelComandasEletronicas.ViewModels;
using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.Controllers
{
    public class RegistroController : BaseController
    {
        private readonly IRegistroHospedeService _registroHospedeService;
        private readonly ILogger<RegistroController> _logger;

        public RegistroController(IRegistroHospedeService registroHospedeService, ILogger<RegistroController> logger)
        {
            _registroHospedeService = registroHospedeService;
            _logger = logger;
        }

        #region Views Principais

        /// <summary>
        /// Lista todos os registros de h�spedes - Recep��o/Supervisor
        /// </summary>
        [HttpGet]
        [RequireRecepcaoOuSupervisor]
        public async Task<IActionResult> Index(string? busca = null, string? status = null)
        {
            try
            {
                List<RegistroHospede> registros;

                if (!string.IsNullOrWhiteSpace(busca))
                {
                    // Busca inteligente
                    registros = await _registroHospedeService.BuscarPorTextoAsync(busca);
                }
                else if (!string.IsNullOrWhiteSpace(status))
                {
                    // Filtrar por status
                    if (status == "Ativo")
                        registros = await _registroHospedeService.ListarAtivosAsync();
                    else if (status == "Finalizado")
                        registros = await _registroHospedeService.ListarFinalizadosAsync();
                    else
                        registros = await _registroHospedeService.ListarTodosAsync();
                }
                else
                {
                    // Listar todos
                    registros = await _registroHospedeService.ListarTodosAsync();
                }

                ViewBag.Busca = busca;
                ViewBag.Status = status;
                ViewBag.Estatisticas = await _registroHospedeService.ObterEstatisticasAsync();

                LogarAcao("ConsultarRegistros", $"Consulta com filtro: busca='{busca}', status='{status}'");

                return View(registros);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar registros de h�spedes");
                DefinirMensagemErro("Erro ao carregar registros. Tente novamente.");
                return View(new List<RegistroHospede>());
            }
        }

        /// <summary>
        /// Formul�rio para cadastrar novo h�spede - Recep��o/Supervisor
        /// </summary>
        [HttpGet]
        [RequireRecepcaoOuSupervisor]
        public IActionResult Cadastrar()
        {
            return View(new RegistroHospedeViewModel());
        }

        /// <summary>
        /// Processar cadastro de novo h�spede - Recep��o/Supervisor
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRecepcaoOuSupervisor]
        public async Task<IActionResult> Cadastrar(RegistroHospedeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Verificar se quarto j� existe
                if (await _registroHospedeService.QuartoJaExisteAsync(model.NumeroQuarto))
                {
                    ModelState.AddModelError("NumeroQuarto", "Este quarto j� est� ocupado.");
                    return View(model);
                }

                var registro = model.ParaModel();
                registro.UsuarioRegistro = UsuarioLogado?.Login ?? CodigoUsuarioAtual ?? "Sistema";

                var sucesso = await _registroHospedeService.CriarRegistroAsync(registro);

                if (sucesso)
                {
                    LogarAcao("CadastrarHospede", 
                        $"Quarto: {model.NumeroQuarto} - Cliente: {model.NomeCliente}",
                        "REGISTROS_HOSPEDE", registro.ID);

                    return RedirecionarComSucesso("Registro", "Index", 
                        $"H�spede {model.NomeCliente} registrado com sucesso no quarto {model.NumeroQuarto}!");
                }
                else
                {
                    DefinirMensagemErro("Erro ao registrar h�spede. Verifique se o quarto n�o est� ocupado.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cadastrar h�spede");
                DefinirMensagemErro("Erro interno ao cadastrar h�spede. Contate o suporte.");
                return View(model);
            }
        }

        /// <summary>
        /// Formul�rio para editar h�spede - Recep��o/Supervisor
        /// </summary>
        [HttpGet]
        [RequireRecepcaoOuSupervisor]
        public async Task<IActionResult> Editar(int id)
        {
            var registro = await _registroHospedeService.BuscarPorIdAsync(id);
            if (registro == null)
            {
                return RedirecionarComErro("Registro", "Index", "Registro n�o encontrado.");
            }

            ViewBag.TemConsumosAtivos = await _registroHospedeService.TemConsumosAtivosAsync(id);

            return View(RegistroHospedeViewModel.DeModel(registro));
        }

        /// <summary>
        /// Processar edi��o de h�spede - Recep��o/Supervisor
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRecepcaoOuSupervisor]
        public async Task<IActionResult> Editar(RegistroHospedeViewModel model)
        {
            ViewBag.TemConsumosAtivos = await _registroHospedeService.TemConsumosAtivosAsync(model.ID);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Verificar se novo quarto j� existe (exceto o pr�prio registro)
                if (await _registroHospedeService.QuartoJaExisteAsync(model.NumeroQuarto, model.ID))
                {
                    ModelState.AddModelError("NumeroQuarto", "Este quarto j� est� ocupado por outro h�spede.");
                    return View(model);
                }

                var registro = model.ParaModel();
                var sucesso = await _registroHospedeService.AlterarRegistroAsync(registro);

                if (sucesso)
                {
                    LogarAcao("AlterarHospede", 
                        $"ID: {model.ID} - Quarto: {model.NumeroQuarto} - Cliente: {model.NomeCliente}",
                        "REGISTROS_HOSPEDE", model.ID);

                    return RedirecionarComSucesso("Registro", "Index", 
                        $"Dados do h�spede {model.NomeCliente} alterados com sucesso!");
                }
                else
                {
                    DefinirMensagemErro("Erro ao alterar dados do h�spede. Verifique se o quarto n�o est� ocupado.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao alterar h�spede");
                DefinirMensagemErro("Erro interno ao alterar h�spede. Contate o suporte.");
                return View(model);
            }
        }

        /// <summary>
        /// Ver detalhes do h�spede e consumos - Recep��o/Supervisor
        /// </summary>
        [HttpGet]
        [RequireRecepcaoOuSupervisor]
        public async Task<IActionResult> Detalhes(int id)
        {
            var registro = await _registroHospedeService.BuscarPorIdAsync(id);
            if (registro == null)
            {
                return RedirecionarComErro("Registro", "Index", "Registro n�o encontrado.");
            }

            ViewBag.PodeFinalizar = await _registroHospedeService.PodeFinalizarAsync(id);
            ViewBag.TemConsumosAtivos = await _registroHospedeService.TemConsumosAtivosAsync(id);

            return View(registro);
        }

        #endregion

        #region A��es AJAX

        /// <summary>
        /// Finalizar registro do h�spede - Recep��o/Supervisor
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRecepcaoOuSupervisor]
        public async Task<IActionResult> Finalizar(int id)
        {
            try
            {
                var registro = await _registroHospedeService.BuscarPorIdAsync(id);
                if (registro == null)
                {
                    return Json(new { sucesso = false, mensagem = "Registro n�o encontrado." });
                }

                if (!registro.IsAtivo())
                {
                    return Json(new { sucesso = false, mensagem = "Registro j� foi finalizado." });
                }

                var usuarioFinalizacao = UsuarioLogado?.Login ?? CodigoUsuarioAtual ?? "Sistema";
                var sucesso = await _registroHospedeService.FinalizarRegistroAsync(id, usuarioFinalizacao);

                if (sucesso)
                {
                    LogarAcao("FinalizarHospede", 
                        $"Quarto: {registro.NumeroQuarto} - Cliente: {registro.NomeCliente} - Total: {registro.ValorGastoTotal:C}",
                        "REGISTROS_HOSPEDE", id);

                    return Json(new { sucesso = true, mensagem = "Registro finalizado com sucesso." });
                }
                else
                {
                    return Json(new { sucesso = false, mensagem = "Erro ao finalizar registro." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao finalizar registro ID: {Id}", id);
                return Json(new { sucesso = false, mensagem = "Erro interno ao finalizar registro." });
            }
        }

        /// <summary>
        /// Buscar h�spedes por termo (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> BuscarPorTermo(string termo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(termo))
                {
                    return Json(new List<object>());
                }

                var registros = await _registroHospedeService.BuscarPorTextoAsync(termo);

                var resultado = registros.Take(10).Select(r => new
                {
                    id = r.ID,
                    numeroQuarto = r.NumeroQuarto,
                    nomeCliente = r.NomeCliente,
                    telefone = r.TelefoneCliente,
                    valorGasto = r.ValorGastoTotal,
                    status = r.Status,
                    display = $"Quarto {r.NumeroQuarto} - {r.NomeCliente}"
                }).ToList();

                return Json(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar por termo: {Termo}", termo);
                return Json(new List<object>());
            }
        }

        /// <summary>
        /// Verificar se quarto j� existe (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> VerificarQuarto(string numeroQuarto, int? excluirId = null)
        {
            try
            {
                var existe = await _registroHospedeService.QuartoJaExisteAsync(numeroQuarto, excluirId);
                return Json(new { existe = existe });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar quarto: {Quarto}", numeroQuarto);
                return Json(new { erro = "Erro ao verificar quarto" });
            }
        }

        /// <summary>
        /// Atualizar valor gasto do h�spede (AJAX)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRecepcaoOuSupervisor]
        public async Task<IActionResult> AtualizarValorGasto(int id)
        {
            try
            {
                await _registroHospedeService.AtualizarValorGastoAsync(id);
                
                var registro = await _registroHospedeService.BuscarPorIdAsync(id);
                if (registro == null)
                {
                    return Json(new { sucesso = false, mensagem = "Registro n�o encontrado." });
                }

                return Json(new { 
                    sucesso = true, 
                    valorAtualizado = registro.ValorGastoTotal,
                    valorFormatado = registro.ValorGastoTotal.ToString("C2")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar valor gasto ID: {Id}", id);
                return Json(new { sucesso = false, mensagem = "Erro ao atualizar valor." });
            }
        }

        #endregion

        #region APIs para outras funcionalidades

        /// <summary>
        /// Listar h�spedes ativos para sele��o (usado no lan�amento)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ListarAtivos()
        {
            try
            {
                var hospedes = await _registroHospedeService.ListarAtivosAsync();
                
                var resultado = hospedes.Select(h => new
                {
                    id = h.ID,
                    numeroQuarto = h.NumeroQuarto,
                    nomeCliente = h.NomeCliente,
                    telefone = h.TelefoneCliente,
                    valorGasto = h.ValorGastoTotal,
                    display = $"Quarto {h.NumeroQuarto} - {h.NomeCliente}"
                }).ToList();

                return Json(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar h�spedes ativos");
                return Json(new List<object>());
            }
        }

        /// <summary>
        /// Obter estat�sticas gerais (AJAX)
        /// </summary>
        [HttpGet]
        [RequireRecepcaoOuSupervisor]
        public async Task<IActionResult> ObterEstatisticas()
        {
            try
            {
                var estatisticas = await _registroHospedeService.ObterEstatisticasAsync();
                return Json(estatisticas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter estat�sticas");
                return Json(new { erro = "Erro ao obter estat�sticas" });
            }
        }

        #endregion
    }
}