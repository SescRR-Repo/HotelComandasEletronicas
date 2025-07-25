using System.ComponentModel.DataAnnotations;
using HotelComandasEletronicas.Models; // ✅ ADICIONAR ESTA LINHA

namespace HotelComandasEletronicas.ViewModels
{
    public class ConsultaViewModel
    {
        [Display(Name = "Método de Consulta")]
        public string MetodoConsulta { get; set; } = "Quarto"; // "Quarto" ou "NomeTelefone"

        [Display(Name = "Número do Quarto")]
        public string? NumeroQuarto { get; set; }

        [Display(Name = "Nome do Hóspede")]
        public string? Nome { get; set; }

        [Display(Name = "Telefone")]
        public string? Telefone { get; set; }

        // Resultado da consulta
        public ConsultaResultadoViewModel? Resultado { get; set; }
        public ExtratoConsultaViewModel? Extrato { get; set; }

        // Estados da consulta
        public bool ConsultaRealizada { get; set; } = false;
        public bool ResultadoEncontrado { get; set; } = false;
        public string? MensagemErro { get; set; }

        // Métodos de validação
        public bool IsConsultaPorQuarto => MetodoConsulta == "Quarto";
        public bool IsConsultaPorNomeTelefone => MetodoConsulta == "NomeTelefone";
        
        public bool TemDadosConsultaQuarto => !string.IsNullOrWhiteSpace(NumeroQuarto);
        public bool TemDadosConsultaNomeTelefone => !string.IsNullOrWhiteSpace(Nome) && !string.IsNullOrWhiteSpace(Telefone);
    }

    public class ConsultaResultadoViewModel
    {
        public int RegistroId { get; set; }
        public string NumeroQuarto { get; set; } = string.Empty;
        public string NomeCliente { get; set; } = string.Empty;
        public string TelefoneCliente { get; set; } = string.Empty; // Já mascarado
        public DateTime DataCheckIn { get; set; }
        public decimal ValorTotalGasto { get; set; }
        public string Status { get; set; } = string.Empty;
        public int DiasHospedado { get; set; }
        public int TotalItensConsumidos { get; set; }
        public string MetodoConsulta { get; set; } = string.Empty;

        // Propriedades computadas
        public string ValorTotalFormatado => ValorTotalGasto.ToString("C2");
        public string DataCheckInFormatada => DataCheckIn.ToString("dd/MM/yyyy HH:mm");
        public string TempoHospedagem => DiasHospedado == 1 ? "1 dia" : $"{DiasHospedado} dias";
        public bool TemConsumo => TotalItensConsumidos > 0;
    }

    public class ExtratoConsultaViewModel
    {
        public int RegistroId { get; set; }
        public string NumeroQuarto { get; set; } = string.Empty;
        public string NomeCliente { get; set; } = string.Empty;
        public DateTime DataCheckIn { get; set; }
        public int DiasHospedado { get; set; }
        public decimal ValorTotalGasto { get; set; }
        public int TotalItens { get; set; }
        public DateTime? UltimoConsumo { get; set; }
        public decimal MediaValorPorItem { get; set; }
        public DateTime DataGeracaoExtrato { get; set; }

        // Totais por categoria
        public decimal TotalBebidas { get; set; }
        public decimal TotalComidas { get; set; }
        public decimal TotalServicos { get; set; }

        // Lista de consumos
        public List<LancamentoConsumo> Consumos { get; set; } = new List<LancamentoConsumo>();

        // Propriedades computadas
        public string ValorTotalFormatado => ValorTotalGasto.ToString("C2");
        public string MediaValorFormatada => MediaValorPorItem.ToString("C2");
        public string TotalBebidasFormatado => TotalBebidas.ToString("C2");
        public string TotalComidasFormatado => TotalComidas.ToString("C2");
        public string TotalServicosFormatado => TotalServicos.ToString("C2");
        public string UltimoConsumoFormatado => UltimoConsumo?.ToString("dd/MM/yyyy HH:mm") ?? "Nenhum";
        public bool TemConsumos => Consumos.Any();
    }
}
