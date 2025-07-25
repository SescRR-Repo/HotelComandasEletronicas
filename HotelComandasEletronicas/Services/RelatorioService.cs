using Microsoft.EntityFrameworkCore;
using HotelComandasEletronicas.Data;
using HotelComandasEletronicas.ViewModels;
using System.Text.Json;

namespace HotelComandasEletronicas.Services
{
    /// <summary>
    /// Serviço para geração de relatórios e estatísticas do sistema
    /// </summary>
    public class RelatorioService : IRelatorioService
    {
        private readonly ComandasDbContext _context;
        private readonly ILogger<RelatorioService> _logger;

        public RelatorioService(ComandasDbContext context, ILogger<RelatorioService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Estatísticas Gerais

        public async Task<EstatisticasGeraisViewModel> ObterEstatisticasGeraisAsync()
        {
            try
            {
                var hoje = DateTime.Today;
                var ontem = hoje.AddDays(-1);
                var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
                var mesPassado = inicioMes.AddMonths(-1);
                var fimMesPassado = inicioMes.AddDays(-1);

                var estatisticas = new EstatisticasGeraisViewModel
                {
                    // Estatísticas do dia
                    VendasHoje = await _context.LancamentosConsumo
                        .Where(l => l.DataHoraLancamento.Date == hoje && l.Status == "Ativo")
                        .SumAsync(l => l.ValorTotal),

                    LancamentosHoje = await _context.LancamentosConsumo
                        .Where(l => l.DataHoraLancamento.Date == hoje && l.Status == "Ativo")
                        .CountAsync(),

                    VendasOntem = await _context.LancamentosConsumo
                        .Where(l => l.DataHoraLancamento.Date == ontem && l.Status == "Ativo")
                        .SumAsync(l => l.ValorTotal),

                    // Estatísticas do mês
                    VendasMesAtual = await _context.LancamentosConsumo
                        .Where(l => l.DataHoraLancamento >= inicioMes && l.Status == "Ativo")
                        .SumAsync(l => l.ValorTotal),

                    VendasMesPassado = await _context.LancamentosConsumo
                        .Where(l => l.DataHoraLancamento >= mesPassado && l.DataHoraLancamento <= fimMesPassado && l.Status == "Ativo")
                        .SumAsync(l => l.ValorTotal),

                    // Estatísticas gerais
                    TotalHospedesAtivos = await _context.RegistrosHospede
                        .Where(r => r.Status == "Ativo")
                        .CountAsync(),

                    TotalProdutosAtivos = await _context.Produtos
                        .Where(p => p.Status)
                        .CountAsync(),

                    TotalUsuariosAtivos = await _context.Usuarios
                        .Where(u => u.Status)
                        .CountAsync(),

                    MediaVendaDiaria = await CalcularMediaVendaDiariaAsync(30),

                    // Ticket médio
                    TicketMedioHoje = await CalcularTicketMedioAsync(hoje, hoje),
                    TicketMedioMes = await CalcularTicketMedioAsync(inicioMes, hoje),

                    // Cancelamentos
                    CancelamentosHoje = await _context.LancamentosConsumo
                        .Where(l => l.DataHoraLancamento.Date == hoje && l.Status == "Cancelado")
                        .CountAsync(),

                    TaxaCancelamentoMes = await CalcularTaxaCancelamentoAsync(inicioMes, hoje)
                };

                // Calcular crescimentos
                estatisticas.CrescimentoVendasDiario = CalcularPercentualCrescimento(estatisticas.VendasOntem, estatisticas.VendasHoje);
                estatisticas.CrescimentoVendasMensal = CalcularPercentualCrescimento(estatisticas.VendasMesPassado, estatisticas.VendasMesAtual);

                return estatisticas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter estatísticas gerais");
                return new EstatisticasGeraisViewModel();
            }
        }

        public async Task<List<VendaRecenteViewModel>> ObterVendasRecentesAsync(int dias)
        {
            try
            {
                var dataInicio = DateTime.Today.AddDays(-dias);

                var vendasRecentes = await _context.LancamentosConsumo
                    .Where(l => l.DataHoraLancamento >= dataInicio && l.Status == "Ativo")
                    .Include(l => l.Produto)
                    .Include(l => l.RegistroHospede)
                    .GroupBy(l => l.DataHoraLancamento.Date)
                    .Select(g => new VendaRecenteViewModel
                    {
                        Data = g.Key,
                        TotalVendas = g.Sum(l => l.ValorTotal),
                        QuantidadeLancamentos = g.Count(),
                        TicketMedio = g.Average(l => l.ValorTotal)
                    })
                    .OrderBy(v => v.Data)
                    .ToListAsync();

                return vendasRecentes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter vendas recentes");
                return new List<VendaRecenteViewModel>();
            }
        }

        #endregion

        #region Relatórios de Vendas

        public async Task<List<VendaPorPeriodoViewModel>> ObterVendasPorPeriodoAsync(DateTime inicio, DateTime fim, string agrupamento)
        {
            try
            {
                var query = _context.LancamentosConsumo
                    .Where(l => l.DataHoraLancamento >= inicio && l.DataHoraLancamento <= fim.AddDays(1) && l.Status == "Ativo")
                    .Include(l => l.Produto);

                List<VendaPorPeriodoViewModel> resultado;

                switch (agrupamento.ToLower())
                {
                    case "horario":
                        resultado = await query
                            .GroupBy(l => new { Data = l.DataHoraLancamento.Date, Hora = l.DataHoraLancamento.Hour })
                            .Select(g => new VendaPorPeriodoViewModel
                            {
                                Periodo = g.Key.Data.ToString("dd/MM") + " " + g.Key.Hora.ToString("00") + "h",
                                TotalVendas = g.Sum(l => l.ValorTotal),
                                QuantidadeLancamentos = g.Count(),
                                TicketMedio = g.Average(l => l.ValorTotal),
                                DataReferencia = g.Key.Data.AddHours(g.Key.Hora)
                            })
                            .OrderBy(v => v.DataReferencia)
                            .ToListAsync();
                        break;

                    case "semanal":
                        var lancamentosSemana = await query.ToListAsync();
                        
                        // Processar agrupamento semanal em memória
                        resultado = lancamentosSemana
                            .GroupBy(l => new 
                            { 
                                Ano = l.DataHoraLancamento.Year,
                                Semana = GetWeekOfYear(l.DataHoraLancamento)
                            })
                            .Select(g => new VendaPorPeriodoViewModel
                            {
                                Periodo = $"Semana {g.Key.Semana}/{g.Key.Ano}",
                                TotalVendas = g.Sum(l => l.ValorTotal),
                                QuantidadeLancamentos = g.Count(),
                                TicketMedio = g.Average(l => l.ValorTotal),
                                DataReferencia = new DateTime(g.Key.Ano, 1, 1).AddDays((g.Key.Semana - 1) * 7)
                            })
                            .OrderBy(v => v.DataReferencia)
                            .ToList();
                        break;

                    case "mensal":
                        resultado = await query
                            .GroupBy(l => new { Ano = l.DataHoraLancamento.Year, Mes = l.DataHoraLancamento.Month })
                            .Select(g => new VendaPorPeriodoViewModel
                            {
                                Periodo = $"{g.Key.Mes:00}/{g.Key.Ano}",
                                TotalVendas = g.Sum(l => l.ValorTotal),
                                QuantidadeLancamentos = g.Count(),
                                TicketMedio = g.Average(l => l.ValorTotal),
                                DataReferencia = new DateTime(g.Key.Ano, g.Key.Mes, 1)
                            })
                            .OrderBy(v => v.DataReferencia)
                            .ToListAsync();
                        break;

                    default: // diario
                        resultado = await query
                            .GroupBy(l => l.DataHoraLancamento.Date)
                            .Select(g => new VendaPorPeriodoViewModel
                            {
                                Periodo = g.Key.ToString("dd/MM/yyyy"),
                                TotalVendas = g.Sum(l => l.ValorTotal),
                                QuantidadeLancamentos = g.Count(),
                                TicketMedio = g.Average(l => l.ValorTotal),
                                DataReferencia = g.Key
                            })
                            .OrderBy(v => v.DataReferencia)
                            .ToListAsync();
                        break;
                }

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter vendas por período");
                return new List<VendaPorPeriodoViewModel>();
            }
        }

        public async Task<ResumoVendasViewModel> ObterResumoVendasAsync(DateTime inicio, DateTime fim)
        {
            try
            {
                var lancamentos = await _context.LancamentosConsumo
                    .Where(l => l.DataHoraLancamento >= inicio && l.DataHoraLancamento <= fim.AddDays(1) && l.Status == "Ativo")
                    .ToListAsync();

                var cancelados = await _context.LancamentosConsumo
                    .Where(l => l.DataHoraLancamento >= inicio && l.DataHoraLancamento <= fim.AddDays(1) && l.Status == "Cancelado")
                    .ToListAsync();

                var resumo = new ResumoVendasViewModel
                {
                    TotalVendas = lancamentos.Sum(l => l.ValorTotal),
                    QuantidadeLancamentos = lancamentos.Count,
                    TicketMedio = lancamentos.Any() ? lancamentos.Average(l => l.ValorTotal) : 0,
                    MaiorVenda = lancamentos.Any() ? lancamentos.Max(l => l.ValorTotal) : 0,
                    MenorVenda = lancamentos.Any() ? lancamentos.Min(l => l.ValorTotal) : 0,
                    TotalCancelamentos = cancelados.Sum(l => l.ValorTotal),
                    QuantidadeCancelamentos = cancelados.Count,
                    TaxaCancelamento = lancamentos.Count + cancelados.Count > 0 
                        ? (decimal)cancelados.Count / (lancamentos.Count + cancelados.Count) * 100 
                        : 0,
                    DiasComVendas = lancamentos.GroupBy(l => l.DataHoraLancamento.Date).Count(),
                    MediaVendasPorDia = 0
                };

                // Calcular média de vendas por dia
                if (resumo.DiasComVendas > 0)
                {
                    resumo.MediaVendasPorDia = resumo.TotalVendas / resumo.DiasComVendas;
                }

                return resumo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter resumo de vendas");
                return new ResumoVendasViewModel();
            }
        }

        public async Task<List<VendaPorCategoriaViewModel>> ObterVendasPorCategoriaAsync(DateTime inicio, DateTime fim)
        {
            try
            {
                var vendasPorCategoria = await _context.LancamentosConsumo
                    .Where(l => l.DataHoraLancamento >= inicio && l.DataHoraLancamento <= fim.AddDays(1) && l.Status == "Ativo")
                    .Include(l => l.Produto)
                    .GroupBy(l => l.Produto.Categoria)
                    .Select(g => new VendaPorCategoriaViewModel
                    {
                        Categoria = g.Key,
                        TotalVendas = g.Sum(l => l.ValorTotal),
                        QuantidadeLancamentos = g.Count(),
                        QuantidadeItens = g.Sum(l => (int)l.Quantidade),
                        TicketMedio = g.Average(l => l.ValorTotal),
                        ProdutoMaisVendido = g.GroupBy(l => l.Produto.Descricao)
                            .OrderByDescending(pg => pg.Sum(l => l.Quantidade))
                            .Select(pg => pg.Key)
                            .FirstOrDefault() ?? ""
                    })
                    .OrderByDescending(v => v.TotalVendas)
                    .ToListAsync();

                // Calcular percentuais
                var totalGeral = vendasPorCategoria.Sum(v => v.TotalVendas);
                foreach (var venda in vendasPorCategoria)
                {
                    venda.PercentualVendas = totalGeral > 0 ? (venda.TotalVendas / totalGeral) * 100 : 0;
                }

                return vendasPorCategoria;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter vendas por categoria");
                return new List<VendaPorCategoriaViewModel>();
            }
        }

        #endregion

        #region Relatórios de Produtos

        public async Task<List<ProdutoMaisVendidoViewModel>> ObterProdutosMaisVendidosAsync(DateTime inicio, DateTime fim, int top = 10)
        {
            try
            {
                var produtosMaisVendidos = await _context.LancamentosConsumo
                    .Where(l => l.DataHoraLancamento >= inicio && l.DataHoraLancamento <= fim.AddDays(1) && l.Status == "Ativo")
                    .Include(l => l.Produto)
                    .GroupBy(l => new { l.Produto.ID, l.Produto.Descricao, l.Produto.Categoria, l.Produto.Valor })
                    .Select(g => new ProdutoMaisVendidoViewModel
                    {
                        ProdutoId = g.Key.ID,
                        NomeProduto = g.Key.Descricao,
                        Categoria = g.Key.Categoria,
                        QuantidadeVendida = (int)g.Sum(l => l.Quantidade),
                        TotalVendas = g.Sum(l => l.ValorTotal),
                        NumeroLancamentos = g.Count(),
                        ValorUnitario = g.Key.Valor,
                        TicketMedio = g.Average(l => l.ValorTotal)
                    })
                    .OrderByDescending(p => p.QuantidadeVendida)
                    .Take(top)
                    .ToListAsync();

                // Adicionar ranking
                for (int i = 0; i < produtosMaisVendidos.Count; i++)
                {
                    produtosMaisVendidos[i].Ranking = i + 1;
                }

                return produtosMaisVendidos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter produtos mais vendidos");
                return new List<ProdutoMaisVendidoViewModel>();
            }
        }

        public async Task<List<ProdutoPorCategoriaViewModel>> ObterProdutosPorCategoriaAsync(string categoria, DateTime inicio, DateTime fim)
        {
            try
            {
                var produtos = await _context.LancamentosConsumo
                    .Where(l => l.DataHoraLancamento >= inicio && l.DataHoraLancamento <= fim.AddDays(1) && 
                               l.Status == "Ativo" && l.Produto.Categoria == categoria)
                    .Include(l => l.Produto)
                    .GroupBy(l => new { l.Produto.ID, l.Produto.Descricao, l.Produto.Valor })
                    .Select(g => new ProdutoPorCategoriaViewModel
                    {
                        ProdutoId = g.Key.ID,
                        NomeProduto = g.Key.Descricao,
                        Categoria = categoria,
                        QuantidadeVendida = (int)g.Sum(l => l.Quantidade),
                        TotalVendas = g.Sum(l => l.ValorTotal),
                        ValorUnitario = g.Key.Valor,
                        NumeroLancamentos = g.Count()
                    })
                    .OrderByDescending(p => p.QuantidadeVendida)
                    .ToListAsync();

                return produtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter produtos por categoria");
                return new List<ProdutoPorCategoriaViewModel>();
            }
        }

        public async Task<List<string>> ObterCategoriasAsync()
        {
            try
            {
                var categorias = await _context.Produtos
                    .Where(p => p.Status)
                    .Select(p => p.Categoria)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return categorias;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter categorias");
                return new List<string>();
            }
        }

        public async Task<List<ProdutoMaisCanceladoViewModel>> ObterProdutosMaisCanceladosAsync(DateTime inicio, DateTime fim, int top = 10)
        {
            try
            {
                var produtosCancelados = await _context.LancamentosConsumo
                    .Where(l => l.DataHoraLancamento >= inicio && l.DataHoraLancamento <= fim.AddDays(1) && l.Status == "Cancelado")
                    .Include(l => l.Produto)
                    .GroupBy(l => new { l.Produto.ID, l.Produto.Descricao, l.Produto.Categoria })
                    .Select(g => new ProdutoMaisCanceladoViewModel
                    {
                        ProdutoId = g.Key.ID,
                        NomeProduto = g.Key.Descricao,
                        Categoria = g.Key.Categoria,
                        QuantidadeCancelada = g.Sum(l => (int)l.Quantidade),
                        ValorCancelado = g.Sum(l => l.ValorTotal),
                        NumeroCancelamentos = g.Count()
                    })
                    .OrderByDescending(p => p.QuantidadeCancelada)
                    .Take(top)
                    .ToListAsync();

                return produtosCancelados;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter produtos mais cancelados");
                return new List<ProdutoMaisCanceladoViewModel>();
            }
        }

        #endregion

        #region Relatórios de Usuários

        public async Task<List<DesempenhoUsuarioViewModel>> ObterDesempenhoPorUsuarioAsync(DateTime inicio, DateTime fim)
        {
            try
            {
                var desempenho = await _context.LancamentosConsumo
                    .Where(l => l.DataHoraLancamento >= inicio && l.DataHoraLancamento <= fim.AddDays(1) && l.Status == "Ativo")
                    .GroupBy(l => l.CodigoUsuarioLancamento)
                    .Select(g => new DesempenhoUsuarioViewModel
                    {
                        CodigoUsuario = g.Key,
                        TotalVendas = g.Sum(l => l.ValorTotal),
                        QuantidadeLancamentos = g.Count(),
                        QuantidadeItens = (int)g.Sum(l => l.Quantidade),
                        TicketMedio = g.Average(l => l.ValorTotal),
                        MaiorVenda = g.Max(l => l.ValorTotal),
                        PrimeiroPedido = g.Min(l => l.DataHoraLancamento),
                        UltimoPedido = g.Max(l => l.DataHoraLancamento)
                    })
                    .OrderByDescending(d => d.TotalVendas)
                    .ToListAsync();

                // Buscar nomes dos usuários
                var codigosUsuarios = desempenho.Select(d => d.CodigoUsuario).ToList();
                var usuarios = await _context.Usuarios
                    .Where(u => codigosUsuarios.Contains(u.CodigoID))
                    .ToDictionaryAsync(u => u.CodigoID, u => u.Nome);

                foreach (var item in desempenho)
                {
                    item.NomeUsuario = usuarios.GetValueOrDefault(item.CodigoUsuario, "Usuário não encontrado");
                    
                    // Calcular dias trabalhados
                    var diasTrabalhados = await _context.LancamentosConsumo
                        .Where(l => l.CodigoUsuarioLancamento == item.CodigoUsuario && 
                                   l.DataHoraLancamento >= inicio && l.DataHoraLancamento <= fim.AddDays(1))
                        .Select(l => l.DataHoraLancamento.Date)
                        .Distinct()
                        .CountAsync();

                    item.DiasTrabalhados = diasTrabalhados;
                    item.MediaVendasPorDia = diasTrabalhados > 0 ? item.TotalVendas / diasTrabalhados : 0;
                }

                // Adicionar ranking
                for (int i = 0; i < desempenho.Count; i++)
                {
                    desempenho[i].Ranking = i + 1;
                }

                return desempenho;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter desempenho por usuário");
                return new List<DesempenhoUsuarioViewModel>();
            }
        }

        public async Task<List<LancamentoPorUsuarioViewModel>> ObterLancamentosPorUsuarioAsync(DateTime inicio, DateTime fim)
        {
            try
            {
                var lancamentos = await _context.LancamentosConsumo
                    .Where(l => l.DataHoraLancamento >= inicio && l.DataHoraLancamento <= fim.AddDays(1))
                    .GroupBy(l => new { l.CodigoUsuarioLancamento, Status = l.Status })
                    .Select(g => new
                    {
                        CodigoUsuario = g.Key.CodigoUsuarioLancamento,
                        Status = g.Key.Status,
                        Quantidade = g.Count(),
                        Valor = g.Sum(l => l.ValorTotal)
                    })
                    .ToListAsync();

                var resultado = lancamentos
                    .GroupBy(l => l.CodigoUsuario)
                    .Select(g => new LancamentoPorUsuarioViewModel
                    {
                        CodigoUsuario = g.Key,
                        LancamentosAtivos = g.Where(x => x.Status == "Ativo").Sum(x => x.Quantidade),
                        LancamentosCancelados = g.Where(x => x.Status == "Cancelado").Sum(x => x.Quantidade),
                        ValorAtivo = g.Where(x => x.Status == "Ativo").Sum(x => x.Valor),
                        ValorCancelado = g.Where(x => x.Status == "Cancelado").Sum(x => x.Valor),
                        TotalLancamentos = g.Sum(x => x.Quantidade)
                    })
                    .ToList();

                // Buscar nomes dos usuários
                var codigosUsuarios = resultado.Select(r => r.CodigoUsuario).ToList();
                var usuarios = await _context.Usuarios
                    .Where(u => codigosUsuarios.Contains(u.CodigoID))
                    .ToDictionaryAsync(u => u.CodigoID, u => u.Nome);

                foreach (var item in resultado)
                {
                    item.NomeUsuario = usuarios.GetValueOrDefault(item.CodigoUsuario, "Usuário não encontrado");
                    item.TaxaCancelamento = item.TotalLancamentos > 0 
                        ? (decimal)item.LancamentosCancelados / item.TotalLancamentos * 100 
                        : 0;
                }

                return resultado.OrderByDescending(r => r.LancamentosAtivos).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter lançamentos por usuário");
                return new List<LancamentoPorUsuarioViewModel>();
            }
        }

        public async Task<DetalhesUsuarioViewModel> ObterDetalhesUsuarioAsync(string codigoUsuario, DateTime inicio, DateTime fim)
        {
            try
            {
                var usuario = await _context.Usuarios
                    .Where(u => u.CodigoID == codigoUsuario)
                    .FirstOrDefaultAsync();

                if (usuario == null)
                {
                    return new DetalhesUsuarioViewModel { CodigoUsuario = codigoUsuario };
                }

                var lancamentos = await _context.LancamentosConsumo
                    .Where(l => l.CodigoUsuarioLancamento == codigoUsuario && 
                               l.DataHoraLancamento >= inicio && l.DataHoraLancamento <= fim.AddDays(1))
                    .Include(l => l.Produto)
                    .Include(l => l.RegistroHospede)
                    .ToListAsync();

                var detalhes = new DetalhesUsuarioViewModel
                {
                    CodigoUsuario = codigoUsuario,
                    NomeUsuario = usuario.Nome,
                    PerfilUsuario = usuario.Perfil,
                    DataCadastro = usuario.DataCadastro,
                    UltimoAcesso = usuario.UltimoAcesso,

                    // Estatísticas do período
                    TotalVendas = lancamentos.Where(l => l.Status == "Ativo").Sum(l => l.ValorTotal),
                    TotalLancamentos = lancamentos.Count(l => l.Status == "Ativo"),
                    TotalCancelamentos = lancamentos.Count(l => l.Status == "Cancelado"),
                    TicketMedio = lancamentos.Where(l => l.Status == "Ativo").Any() 
                        ? lancamentos.Where(l => l.Status == "Ativo").Average(l => l.ValorTotal) 
                        : 0,

                    // Análise temporal
                    DiasTrabalhados = lancamentos.Select(l => l.DataHoraLancamento.Date).Distinct().Count(),
                    PrimeiroPedido = lancamentos.Any() ? lancamentos.Min(l => l.DataHoraLancamento) : null,
                    UltimoPedido = lancamentos.Any() ? lancamentos.Max(l => l.DataHoraLancamento) : null,

                    // Produtos favoritos
                    ProdutoMaisVendido = lancamentos
                        .Where(l => l.Status == "Ativo")
                        .GroupBy(l => l.Produto.Descricao)
                        .OrderByDescending(g => g.Sum(l => l.Quantidade))
                        .Select(g => g.Key)
                        .FirstOrDefault(),

                    CategoriaMaisVendida = lancamentos
                        .Where(l => l.Status == "Ativo")
                        .GroupBy(l => l.Produto.Categoria)
                        .OrderByDescending(g => g.Sum(l => l.ValorTotal))
                        .Select(g => g.Key)
                        .FirstOrDefault() ?? string.Empty,

                    // Performance
                    MelhorDia = lancamentos
                        .Where(l => l.Status == "Ativo")
                        .GroupBy(l => l.DataHoraLancamento.Date)
                        .OrderByDescending(g => g.Sum(l => l.ValorTotal))
                        .Select(g => new { Data = g.Key, Valor = g.Sum(l => l.ValorTotal) })
                        .FirstOrDefault(),

                    HorarioMaisAtivo = lancamentos
                        .Where(l => l.Status == "Ativo")
                        .GroupBy(l => l.DataHoraLancamento.Hour)
                        .OrderByDescending(g => g.Count())
                        .Select(g => g.Key)
                        .FirstOrDefault()
                };

                // Calcular métricas adicionais
                if (detalhes.DiasTrabalhados > 0)
                {
                    detalhes.MediaVendasPorDia = detalhes.TotalVendas / detalhes.DiasTrabalhados;
                    detalhes.MediaLancamentosPorDia = (decimal)detalhes.TotalLancamentos / detalhes.DiasTrabalhados;
                }

                if (detalhes.TotalLancamentos + detalhes.TotalCancelamentos > 0)
                {
                    detalhes.TaxaCancelamento = (decimal)detalhes.TotalCancelamentos / 
                        (detalhes.TotalLancamentos + detalhes.TotalCancelamentos) * 100;
                }

                return detalhes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter detalhes do usuário {CodigoUsuario}", codigoUsuario);
                return new DetalhesUsuarioViewModel { CodigoUsuario = codigoUsuario };
            }
        }

        #endregion

        #region Relatórios de Ocupação

        public async Task<List<OcupacaoPorPeriodoViewModel>> ObterOcupacaoPorPeriodoAsync(DateTime inicio, DateTime fim)
        {
            try
            {
                var ocupacao = await _context.RegistrosHospede
                    .Where(r => r.DataRegistro >= inicio && r.DataRegistro <= fim.AddDays(1))
                    .GroupBy(r => r.DataRegistro.Date)
                    .Select(g => new OcupacaoPorPeriodoViewModel
                    {
                        Data = g.Key,
                        NovasReservas = g.Count(),
                        ReservasAtivas = g.Count(r => r.Status == "Ativo"),
                        ReservasFinalizadas = g.Count(r => r.Status == "Finalizado"),
                        ValorMedioGasto = g.Where(r => r.Status == "Ativo").Average(r => r.ValorGastoTotal),
                        QuartoMaisAtivo = g.OrderByDescending(r => r.ValorGastoTotal).Select(r => r.NumeroQuarto).FirstOrDefault()
                    })
                    .OrderBy(o => o.Data)
                    .ToListAsync();

                return ocupacao;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter ocupação por período");
                return new List<OcupacaoPorPeriodoViewModel>();
            }
        }

        public async Task<List<QuartoMaisAtivoViewModel>> ObterQuartosMaisAtivosAsync(DateTime inicio, DateTime fim, int top = 10)
        {
            try
            {
                var quartosAtivos = await _context.LancamentosConsumo
                    .Where(l => l.DataHoraLancamento >= inicio && l.DataHoraLancamento <= fim.AddDays(1) && l.Status == "Ativo")
                    .Include(l => l.RegistroHospede)
                    .GroupBy(l => new { l.RegistroHospede.NumeroQuarto, l.RegistroHospede.NomeCliente })
                    .Select(g => new QuartoMaisAtivoViewModel
                    {
                        NumeroQuarto = g.Key.NumeroQuarto,
                        NomeHospede = g.Key.NomeCliente,
                        TotalConsumido = g.Sum(l => l.ValorTotal),
                        QuantidadeLancamentos = g.Count(),
                        QuantidadeItens = g.Sum(l => (int)l.Quantidade),
                        TicketMedio = g.Average(l => l.ValorTotal),
                        PrimeiroPedido = g.Min(l => l.DataHoraLancamento),
                        UltimoPedido = g.Max(l => l.DataHoraLancamento)
                    })
                    .OrderByDescending(q => q.TotalConsumido)
                    .Take(top)
                    .ToListAsync();

                // Adicionar ranking e calcular estatísticas adicionais
                for (int i = 0; i < quartosAtivos.Count; i++)
                {
                    quartosAtivos[i].Ranking = i + 1;
                    
                    var diasAtivos = (quartosAtivos[i].UltimoPedido - quartosAtivos[i].PrimeiroPedido).Days + 1;
                    quartosAtivos[i].MediaConsumoPorDia = diasAtivos > 0 ? quartosAtivos[i].TotalConsumido / diasAtivos : 0;
                }

                return quartosAtivos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter quartos mais ativos");
                return new List<QuartoMaisAtivoViewModel>();
            }
        }

        public async Task<TempoMedioEstadiaViewModel> ObterTempoMedioEstadiaAsync(DateTime inicio, DateTime fim)
        {
            try
            {
                var registros = await _context.RegistrosHospede
                    .Where(r => r.DataRegistro >= inicio && r.DataRegistro <= fim.AddDays(1))
                    .ToListAsync();

                var tempoMedio = new TempoMedioEstadiaViewModel
                {
                    TotalRegistros = registros.Count,
                    RegistrosAtivos = registros.Count(r => r.Status == "Ativo"),
                    RegistrosFinalizados = registros.Count(r => r.Status == "Finalizado")
                };

                // Para registros finalizados, podemos calcular o tempo real de estadia
                var registrosFinalizados = registros.Where(r => r.Status == "Finalizado").ToList();

                if (registrosFinalizados.Any())
                {
                    // Como não temos data de checkout, vamos usar a data do último lançamento como aproximação
                    var temposEstadia = new List<double>();

                    foreach (var registro in registrosFinalizados)
                    {
                        var ultimoLancamento = await _context.LancamentosConsumo
                            .Where(l => l.RegistroHospedeID == registro.ID)
                            .OrderByDescending(l => l.DataHoraLancamento)
                            .FirstOrDefaultAsync();

                        if (ultimoLancamento != null)
                        {
                            var tempoEstadia = (ultimoLancamento.DataHoraLancamento - registro.DataRegistro).TotalDays;
                            temposEstadia.Add(tempoEstadia);
                        }
                    }

                    if (temposEstadia.Any())
                    {
                        tempoMedio.TempoMedioEstadia = temposEstadia.Average();
                        tempoMedio.MenorEstadia = temposEstadia.Min();
                        tempoMedio.MaiorEstadia = temposEstadia.Max();
                    }
                }

                // Para registros ativos, calcular tempo atual
                var registrosAtivos = registros.Where(r => r.Status == "Ativo").ToList();
                if (registrosAtivos.Any())
                {
                    var temposAtuais = registrosAtivos.Select(r => (DateTime.Now - r.DataRegistro).TotalDays).ToList();
                    tempoMedio.TempoMedioAtual = temposAtuais.Average();
                }

                return tempoMedio;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter tempo médio de estadia");
                return new TempoMedioEstadiaViewModel();
            }
        }

        #endregion

        #region Relatórios de Cancelamentos

        public async Task<List<CancelamentoPorPeriodoViewModel>> ObterCancelamentosPorPeriodoAsync(DateTime inicio, DateTime fim)
        {
            try
            {
                var cancelamentos = await _context.LancamentosConsumo
                    .Where(l => l.DataCancelamento.HasValue && 
                               l.DataCancelamento >= inicio && l.DataCancelamento <= fim.AddDays(1) && 
                               l.Status == "Cancelado")
                    .GroupBy(l => l.DataCancelamento.Value.Date)
                    .Select(g => new CancelamentoPorPeriodoViewModel
                    {
                        Data = g.Key,
                        QuantidadeCancelamentos = g.Count(),
                        ValorCancelado = g.Sum(l => l.ValorTotal),
                        TicketMedioCancelado = g.Average(l => l.ValorTotal)
                    })
                    .OrderBy(c => c.Data)
                    .ToListAsync();

                // Calcular totais de lançamentos para taxa de cancelamento
                foreach (var cancelamento in cancelamentos)
                {
                    var totalLancamentos = await _context.LancamentosConsumo
                        .Where(l => l.DataHoraLancamento.Date == cancelamento.Data)
                        .CountAsync();

                    cancelamento.TotalLancamentos = totalLancamentos;
                    cancelamento.TaxaCancelamento = totalLancamentos > 0 
                        ? (decimal)cancelamento.QuantidadeCancelamentos / totalLancamentos * 100 
                        : 0;
                }

                return cancelamentos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter cancelamentos por período");
                return new List<CancelamentoPorPeriodoViewModel>();
            }
        }

        public async Task<List<MotivoCancelamentoViewModel>> ObterMotivosCancelamentoAsync(DateTime inicio, DateTime fim)
        {
            try
            {
                var motivos = await _context.LancamentosConsumo
                    .Where(l => l.DataCancelamento.HasValue && 
                               l.DataCancelamento >= inicio && l.DataCancelamento <= fim.AddDays(1) && 
                               l.Status == "Cancelado" &&
                               !string.IsNullOrEmpty(l.ObservacoesCancelamento))
                    .GroupBy(l => l.ObservacoesCancelamento)
                    .Select(g => new MotivoCancelamentoViewModel
                    {
                        Motivo = g.Key,
                        Quantidade = g.Count(),
                        ValorTotal = g.Sum(l => l.ValorTotal)
                    })
                    .OrderByDescending(m => m.Quantidade)
                    .ToListAsync();

                // Calcular percentuais
                var totalCancelamentos = motivos.Sum(m => m.Quantidade);
                foreach (var motivo in motivos)
                {
                    motivo.Percentual = totalCancelamentos > 0 ? (decimal)motivo.Quantidade / totalCancelamentos * 100 : 0;
                }

                return motivos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter motivos de cancelamento");
                return new List<MotivoCancelamentoViewModel>();
            }
        }

        #endregion

        #region Dados para Gráficos

        public async Task<object> ObterDadosGraficoVendasAsync(DateTime inicio, DateTime fim, string agrupamento)
        {
            try
            {
                var vendas = await ObterVendasPorPeriodoAsync(inicio, fim, agrupamento);

                return new
                {
                    labels = vendas.Select(v => v.Periodo).ToArray(),
                    datasets = new[]
                    {
                        new
                        {
                            label = "Vendas (R$)",
                            data = vendas.Select(v => v.TotalVendas).ToArray(),
                            backgroundColor = "rgba(54, 162, 235, 0.5)",
                            borderColor = "rgba(54, 162, 235, 1)",
                            borderWidth = 1
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter dados para gráfico de vendas");
                return new { erro = "Erro ao carregar dados" };
            }
        }

        public async Task<object> ObterDadosGraficoCategoriasAsync(DateTime inicio, DateTime fim)
        {
            try
            {
                var categorias = await ObterVendasPorCategoriaAsync(inicio, fim);

                return new
                {
                    labels = categorias.Select(c => c.Categoria).ToArray(),
                    datasets = new[]
                    {
                        new
                        {
                            label = "Vendas por Categoria",
                            data = categorias.Select(c => c.TotalVendas).ToArray(),
                            backgroundColor = new[]
                            {
                                "rgba(255, 99, 132, 0.5)",
                                "rgba(54, 162, 235, 0.5)",
                                "rgba(255, 205, 86, 0.5)",
                                "rgba(75, 192, 192, 0.5)",
                                "rgba(153, 102, 255, 0.5)"
                            }
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter dados para gráfico de categorias");
                return new { erro = "Erro ao carregar dados" };
            }
        }

        public async Task<object> ObterDadosGraficoUsuariosAsync(DateTime inicio, DateTime fim)
        {
            try
            {
                var usuarios = await ObterDesempenhoPorUsuarioAsync(inicio, fim);
                var top5 = usuarios.Take(5).ToList();

                return new
                {
                    labels = top5.Select(u => u.NomeUsuario ?? u.CodigoUsuario).ToArray(),
                    datasets = new[]
                    {
                        new
                        {
                            label = "Vendas por Usuário (R$)",
                            data = top5.Select(u => u.TotalVendas).ToArray(),
                            backgroundColor = "rgba(153, 102, 255, 0.5)",
                            borderColor = "rgba(153, 102, 255, 1)",
                            borderWidth = 1
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter dados para gráfico de usuários");
                return new { erro = "Erro ao carregar dados" };
            }
        }

        #endregion

        #region Exportação

        public async Task<byte[]> ExportarParaExcelAsync(string tipoRelatorio, DateTime inicio, DateTime fim, string? filtros = null)
        {
            try
            {
                // Implementação simplificada - em produção usaria uma biblioteca como EPPlus
                var dados = await ObterDadosParaExportacao(tipoRelatorio, inicio, fim, filtros);
                var json = JsonSerializer.Serialize(dados, new JsonSerializerOptions { WriteIndented = true });
                
                // Por enquanto, retorna como texto em bytes
                // Em produção, implementaria geração real de Excel
                return System.Text.Encoding.UTF8.GetBytes($"Relatório {tipoRelatorio}\nPeríodo: {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}\n\n{json}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao exportar para Excel");
                return System.Text.Encoding.UTF8.GetBytes("Erro ao gerar relatório");
            }
        }

        public async Task<byte[]> ExportarParaPdfAsync(String tipoRelatorio, DateTime inicio, DateTime fim, string? filtros = null)
        {
            try
            {
                // Implementação simplificada - em produção usaria uma biblioteca como iTextSharp
                var dados = await ObterDadosParaExportacao(tipoRelatorio, inicio, fim, filtros);
                var json = JsonSerializer.Serialize(dados, new JsonSerializerOptions { WriteIndented = true });
                
                // Por enquanto, retorna como texto em bytes
                // Em produção, implementaria geração real de PDF
                return System.Text.Encoding.UTF8.GetBytes($"RELATÓRIO {tipoRelatorio.ToUpper()}\nPeríodo: {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}\n\n{json}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao exportar para PDF");
                return System.Text.Encoding.UTF8.GetBytes("Erro ao gerar relatório");
            }
        }

        #endregion

        #region Relatórios Personalizados

        public async Task<RelatorioPersonalizadoResultadoViewModel> GerarRelatorioPersonalizadoAsync(RelatorioPersonalizadoViewModel configuracao)
        {
            try
            {
                var resultado = new RelatorioPersonalizadoResultadoViewModel
                {
                    TituloRelatorio = configuracao.TituloPersonalizado ?? "Relatório Personalizado",
                    DataGeracao = DateTime.Now,
                    PeriodoInicio = configuracao.DataInicio,
                    PeriodoFim = configuracao.DataFim
                };

                // Implementar lógica baseada nas configurações selecionadas
                // Por enquanto, implementação básica

                if (configuracao.IncluirVendas)
                {
                    resultado.ResumoVendas = await ObterResumoVendasAsync(configuracao.DataInicio, configuracao.DataFim);
                }

                if (configuracao.IncluirProdutos)
                {
                    resultado.ProdutosMaisVendidos = await ObterProdutosMaisVendidosAsync(configuracao.DataInicio, configuracao.DataFim, 10);
                }

                if (configuracao.IncluirUsuarios)
                {
                    resultado.DesempenhoUsuarios = await ObterDesempenhoPorUsuarioAsync(configuracao.DataInicio, configuracao.DataFim);
                }

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar relatório personalizado");
                return new RelatorioPersonalizadoResultadoViewModel
                {
                    TituloRelatorio = "Erro",
                    DataGeracao = DateTime.Now,
                    ErroGeração = "Erro ao processar configurações do relatório"
                };
            }
        }

        #endregion

        #region Métodos Auxiliares Privados

        private static int GetWeekOfYear(DateTime date)
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            var calendar = culture.Calendar;
            var weekRule = culture.DateTimeFormat.CalendarWeekRule;
            var firstDayOfWeek = culture.DateTimeFormat.FirstDayOfWeek;
            
            return calendar.GetWeekOfYear(date, weekRule, firstDayOfWeek);
        }

        private async Task<decimal> CalcularMediaVendaDiariaAsync(int dias)
        {
            try
            {
                var dataInicio = DateTime.Today.AddDays(-dias);
                var vendas = await _context.LancamentosConsumo
                    .Where(l => l.DataHoraLancamento >= dataInicio && l.Status == "Ativo")
                    .GroupBy(l => l.DataHoraLancamento.Date)
                    .Select(g => g.Sum(l => l.ValorTotal))
                    .ToListAsync();

                return vendas.Any() ? vendas.Average() : 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<decimal> CalcularTicketMedioAsync(DateTime inicio, DateTime fim)
        {
            try
            {
                var lancamentos = await _context.LancamentosConsumo
                    .Where(l => l.DataHoraLancamento >= inicio && l.DataHoraLancamento <= fim.AddDays(1) && l.Status == "Ativo")
                    .ToListAsync();

                return lancamentos.Any() ? lancamentos.Average(l => l.ValorTotal) : 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<decimal> CalcularTaxaCancelamentoAsync(DateTime inicio, DateTime fim)
        {
            try
            {
                var totalLancamentos = await _context.LancamentosConsumo
                    .Where(l => l.DataHoraLancamento >= inicio && l.DataHoraLancamento <= fim.AddDays(1))
                    .CountAsync();

                var cancelamentos = await _context.LancamentosConsumo
                    .Where(l => l.DataHoraLancamento >= inicio && l.DataHoraLancamento <= fim.AddDays(1) && l.Status == "Cancelado")
                    .CountAsync();

                return totalLancamentos > 0 ? (decimal)cancelamentos / totalLancamentos * 100 : 0;
            }
            catch
            {
                return 0;
            }
        }

        private static decimal CalcularPercentualCrescimento(decimal valorAnterior, decimal valorAtual)
        {
            if (valorAnterior == 0) return valorAtual > 0 ? 100 : 0;
            return ((valorAtual - valorAnterior) / valorAnterior) * 100;
        }

        private async Task<object> ObterDadosParaExportacao(string tipoRelatorio, DateTime inicio, DateTime fim, string? filtros)
        {
            return tipoRelatorio.ToLower() switch
            {
                "vendas" => await ObterVendasPorPeriodoAsync(inicio, fim, "diario"),
                "produtos" => await ObterProdutosMaisVendidosAsync(inicio, fim, 50),
                "usuarios" => await ObterDesempenhoPorUsuarioAsync(inicio, fim),
                "ocupacao" => await ObterOcupacaoPorPeriodoAsync(inicio, fim),
                "cancelamentos" => await ObterCancelamentosPorPeriodoAsync(inicio, fim),
                _ => new { erro = "Tipo de relatório não suportado" }
            };
        }

        #endregion
    }
}