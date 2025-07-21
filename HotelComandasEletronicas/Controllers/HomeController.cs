using Microsoft.AspNetCore.Mvc;
using HotelComandasEletronicas.Data;
using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ComandasDbContext _context;

        public HomeController(ComandasDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Verificar status do sistema
            ViewBag.TotalUsuarios = _context.Usuarios.Count();
            ViewBag.TotalProdutos = _context.Produtos.Count();
            ViewBag.TotalHospedes = _context.RegistrosHospede.Count();
            ViewBag.TotalLancamentos = _context.LancamentosConsumo.Count();

            // Dados para teste
            ViewBag.UsuariosAtivos = _context.Usuarios.Where(u => u.Status).Take(3).ToList();
            ViewBag.ProdutosDisponiveis = _context.Produtos.Where(p => p.Status).Take(5).ToList();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }

        // Endpoint para testar conexão com banco
        public IActionResult TestarConexao()
        {
            try
            {
                var totalUsuarios = _context.Usuarios.Count();
                var totalProdutos = _context.Produtos.Count();

                DefinirMensagemSucesso($"? Conexão OK! {totalUsuarios} usuários e {totalProdutos} produtos encontrados.");
            }
            catch (Exception ex)
            {
                DefinirMensagemErro($"? Erro na conexão: {ex.Message}");
            }

            return RedirecionarParaHome();
        }

        // Endpoint para resetar dados (apenas para desenvolvimento)
        public IActionResult ResetarDados()
        {
            try
            {
                // Limpar todas as tabelas
                _context.LogsSistema.RemoveRange(_context.LogsSistema);
                _context.LancamentosConsumo.RemoveRange(_context.LancamentosConsumo);
                _context.RegistrosHospede.RemoveRange(_context.RegistrosHospede);
                _context.Produtos.RemoveRange(_context.Produtos);
                _context.Usuarios.RemoveRange(_context.Usuarios);

                _context.SaveChanges();

                // Popular dados iniciais novamente
                _context.PopularDadosIniciais();

                DefinirMensagemSucesso("? Dados resetados e populados com sucesso!");
            }
            catch (Exception ex)
            {
                DefinirMensagemErro($"? Erro ao resetar dados: {ex.Message}");
            }

            return RedirecionarParaHome();
        }
    }
}