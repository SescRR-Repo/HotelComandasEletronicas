using Microsoft.EntityFrameworkCore;
using HotelComandasEletronicas.Data;
using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.Services
{
    public class ConsultaClienteService : IConsultaClienteService
    {
        private readonly ComandasDbContext _context;
        private readonly ILogger<ConsultaClienteService> _logger;

        public ConsultaClienteService(ComandasDbContext context, ILogger<ConsultaClienteService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<RegistroHospede?> ValidarClienteAsync(string quarto, string? nome = null, string? telefone = null)
        {
            // Implementação será feita na próxima etapa
            return await _context.RegistrosHospede
                .FirstOrDefaultAsync(r => r.NumeroQuarto == quarto && r.Status == "Ativo");
        }

        public async Task<RegistroHospede?> BuscarPorQuartoAsync(string numeroQuarto)
        {
            return await _context.RegistrosHospede
                .FirstOrDefaultAsync(r => r.NumeroQuarto == numeroQuarto && r.Status == "Ativo");
        }

        public async Task<RegistroHospede?> BuscarPorNomeETelefoneAsync(string nome, string telefone)
        {
            return await _context.RegistrosHospede
                .FirstOrDefaultAsync(r => r.NomeCliente.Contains(nome) &&
                                        r.TelefoneCliente.Contains(telefone) && r.Status == "Ativo");
        }

        public async Task<List<LancamentoConsumo>> GetExtratoPorQuartoAsync(string numeroQuarto)
        {
            var hospede = await BuscarPorQuartoAsync(numeroQuarto);
            if (hospede == null) return new List<LancamentoConsumo>();

            return await _context.LancamentosConsumo
                .Include(l => l.Produto)
                .Where(l => l.RegistroHospedeID == hospede.ID)
                .OrderByDescending(l => l.DataHoraLancamento)
                .ToListAsync();
        }

        public async Task<List<LancamentoConsumo>> GetExtratoPorHospedeAsync(int hospedeId)
        {
            return await _context.LancamentosConsumo
                .Include(l => l.Produto)
                .Where(l => l.RegistroHospedeID == hospedeId)
                .OrderByDescending(l => l.DataHoraLancamento)
                .ToListAsync();
        }

        public async Task<ExtratoCliente> GerarExtratoClienteAsync(int hospedeId)
        {
            var hospede = await _context.RegistrosHospede.FindAsync(hospedeId);
            if (hospede == null) throw new ArgumentException("Hóspede não encontrado");

            var consumos = await GetExtratoPorHospedeAsync(hospedeId);

            return new ExtratoCliente
            {
                Hospede = hospede,
                Consumos = consumos,
                TotalGasto = consumos.Where(c => c.Status == "Ativo").Sum(c => c.ValorTotal),
                UltimaAtualizacao = DateTime.Now,
                TotalItens = consumos.Count(c => c.Status == "Ativo")
            };
        }

        public async Task<bool> ValidarDadosClienteAsync(string? quarto = null, string? nome = null, string? telefone = null)
        {
            await Task.CompletedTask;
            return !string.IsNullOrWhiteSpace(quarto) ||
                   (!string.IsNullOrWhiteSpace(nome) && !string.IsNullOrWhiteSpace(telefone));
        }
    }
}