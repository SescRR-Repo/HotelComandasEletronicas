using Microsoft.EntityFrameworkCore;
using HotelComandasEletronicas.Data;
using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.Services
{
    public class LancamentoService : ILancamentoService
    {
        private readonly ComandasDbContext _context;
        private readonly ILogger<LancamentoService> _logger;

        public LancamentoService(ComandasDbContext context, ILogger<LancamentoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> RegistrarConsumoAsync(LancamentoConsumo lancamento)
        {
            // Implementação será feita na próxima etapa
            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> CancelarLancamentoAsync(int id, string motivo, string usuarioCancelamento)
        {
            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> RemoverItemCarrinhoAsync(int id)
        {
            await Task.CompletedTask;
            return true;
        }

        public async Task<LancamentoConsumo?> BuscarPorIdAsync(int id)
        {
            return await _context.LancamentosConsumo.FindAsync(id);
        }

        public async Task<List<LancamentoConsumo>> GetConsumosPorHospedeAsync(int hospedeId)
        {
            return await _context.LancamentosConsumo
                .Where(l => l.RegistroHospedeID == hospedeId)
                .ToListAsync();
        }

        public async Task<List<LancamentoConsumo>> GetConsumosPorPeriodoAsync(DateTime inicio, DateTime fim)
        {
            return await _context.LancamentosConsumo
                .Where(l => l.DataHoraLancamento >= inicio && l.DataHoraLancamento <= fim)
                .ToListAsync();
        }

        public async Task<List<LancamentoConsumo>> GetConsumosPorUsuarioAsync(string codigoUsuario)
        {
            return await _context.LancamentosConsumo
                .Where(l => l.CodigoUsuarioLancamento == codigoUsuario)
                .ToListAsync();
        }

        public async Task<List<LancamentoConsumo>> ListarTodosAsync()
        {
            return await _context.LancamentosConsumo.ToListAsync();
        }

        public async Task<decimal> CalcularTotalPeriodoAsync(DateTime inicio, DateTime fim)
        {
            return await _context.LancamentosConsumo
                .Where(l => l.DataHoraLancamento >= inicio && l.DataHoraLancamento <= fim && l.IsAtivo())
                .SumAsync(l => l.ValorTotal);
        }

        public async Task<decimal> CalcularTotalHospedeAsync(int hospedeId)
        {
            return await _context.LancamentosConsumo
                .Where(l => l.RegistroHospedeID == hospedeId && l.IsAtivo())
                .SumAsync(l => l.ValorTotal);
        }

        public async Task<bool> ValidarPermissaoCancelamentoAsync(string codigoUsuario)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.CodigoID == codigoUsuario);
            return usuario?.TemPermissaoCancelamento() ?? false;
        }

        public async Task<bool> PodeCancelarAsync(int lancamentoId, string codigoUsuario)
        {
            var lancamento = await _context.LancamentosConsumo.FindAsync(lancamentoId);
            if (lancamento == null || !lancamento.IsAtivo()) return false;

            return await ValidarPermissaoCancelamentoAsync(codigoUsuario);
        }

        public async Task<Dictionary<string, object>> ObterEstatisticasAsync()
        {
            return new Dictionary<string, object>
            {
                ["TotalLancamentos"] = await _context.LancamentosConsumo.CountAsync(),
                ["LancamentosAtivos"] = await _context.LancamentosConsumo.CountAsync(l => l.IsAtivo()),
                ["ValorTotal"] = await _context.LancamentosConsumo.Where(l => l.IsAtivo()).SumAsync(l => l.ValorTotal)
            };
        }
    }
}
