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
        /// Lista todos os registros de hóspedes - Recepção/Supervisor
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
                _logger.LogError(ex, "Erro ao listar registros de hóspedes");
                DefinirMensagemErro("Erro ao carregar registros. Tente novamente.");
                return View(new List<RegistroHospede>());
            }
        }

        /// <summary>
        /// Formulário para cadastrar novo hóspede - Recepção/Supervisor
        /// </summary>
        [HttpGet]
        [RequireRecepcaoOuSupervisor]
        public IActionResult Cadastrar()
        {
            return View(new RegistroHospedeViewModel());
        }

        /// <summary>
        /// Processar cadastro de novo hóspede - Recepção/Supervisor
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
                // Verificar se quarto já existe
                if (await _registroHospedeService.QuartoJaExisteAsync(model.NumeroQuarto))
                {
                    ModelState.AddModelError("NumeroQuarto", "Este quarto já está ocupado.");
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
                        $"Hóspede {model.NomeCliente} registrado com sucesso no quarto {model.NumeroQuarto}!");
                }
                else
                {
                    DefinirMensagemErro("Erro ao registrar hóspede. Verifique se o quarto não está ocupado.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cadastrar hóspede");
                DefinirMensagemErro("Erro interno ao cadastrar hóspede. Contate o suporte.");
                return View(model);
            }
        }

        /// <summary>
        /// Formulário para editar hóspede - Recepção/Supervisor
        /// </summary>
        [HttpGet]
        [RequireRecepcaoOuSupervisor]
        public async Task<IActionResult> Editar(int id)
        {
            var registro = await _registroHospedeService.BuscarPorIdAsync(id);
            if (registro == null)
            {
                return RedirecionarComErro("Registro", "Index", "Registro não encontrado.");
            }

            ViewBag.TemConsumosAtivos = await _registroHospedeService.TemConsumosAtivosAsync(id);

            return View(RegistroHospedeViewModel.DeModel(registro));
        }

        /// <summary>
        /// Processar edição de hóspede - Recepção/Supervisor
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
                // Verificar se novo quarto já existe (exceto o próprio registro)
                if (await _registroHospedeService.QuartoJaExisteAsync(model.NumeroQuarto, model.ID))
                {
                    ModelState.AddModelError("NumeroQuarto", "Este quarto já está ocupado por outro hóspede.");
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
                        $"Dados do hóspede {model.NomeCliente} alterados com sucesso!");
                }
                else
                {
                    DefinirMensagemErro("Erro ao alterar dados do hóspede. Verifique se o quarto não está ocupado.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao alterar hóspede");
                DefinirMensagemErro("Erro interno ao alterar hóspede. Contate o suporte.");
                return View(model);
            }
        }

        /// <summary>
        /// Ver detalhes do hóspede e consumos - Recepção/Supervisor
        /// </summary>
        [HttpGet]
        [RequireRecepcaoOuSupervisor]
        public async Task<IActionResult> Detalhes(int id)
        {
            var registro = await _registroHospedeService.BuscarPorIdAsync(id);
            if (registro == null)
            {
                return RedirecionarComErro("Registro", "Index", "Registro não encontrado.");
            }

            ViewBag.PodeFinalizar = await _registroHospedeService.PodeFinalizarAsync(id);
            ViewBag.TemConsumosAtivos = await _registroHospedeService.TemConsumosAtivosAsync(id);

            return View(registro);
        }

        #endregion

        #region Ações AJAX

        /// <summary>
        /// Finalizar registro do hóspede - Recepção/Supervisor
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
                    return Json(new { sucesso = false, mensagem = "Registro não encontrado." });
                }

                if (!registro.IsAtivo())
                {
                    return Json(new { sucesso = false, mensagem = "Registro já foi finalizado." });
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
        /// Buscar hóspedes por termo (AJAX)
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
        /// Verificar se quarto já existe (AJAX)
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
        /// Atualizar valor gasto do hóspede (AJAX)
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
                    return Json(new { sucesso = false, mensagem = "Registro não encontrado." });
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
        /// Listar hóspedes ativos para seleção (usado no lançamento)
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
                _logger.LogError(ex, "Erro ao listar hóspedes ativos");
                return Json(new List<object>());
            }
        }

        /// <summary>
        /// Obter estatísticas gerais (AJAX)
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
                _logger.LogError(ex, "Erro ao obter estatísticas");
                return Json(new { erro = "Erro ao obter estatísticas" });
            }
        }

        #endregion
    }
}