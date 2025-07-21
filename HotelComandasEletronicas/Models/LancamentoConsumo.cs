using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelComandasEletronicas.Models
{
    [Table("LANCAMENTOS_CONSUMO")]
    public class LancamentoConsumo
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public DateTime DataHoraLancamento { get; set; } = DateTime.Now;

        [Required]
        public int RegistroHospedeID { get; set; }

        [Required]
        public int ProdutoID { get; set; }

        [Required]
        [Column(TypeName = "decimal(8,2)")]
        public decimal Quantidade { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal ValorUnitario { get; set; } // Preço no momento do lançamento

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal ValorTotal { get; set; } // Calculado automaticamente

        [Required]
        [StringLength(2)]
        public string CodigoUsuarioLancamento { get; set; } = string.Empty; // "03", "18", "01"

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Ativo"; // "Ativo", "Cancelado"

        [StringLength(200)]
        public string? ObservacoesCancelamento { get; set; }

        public DateTime? DataCancelamento { get; set; }

        [StringLength(2)]
        public string? UsuarioCancelamento { get; set; } // Código de quem cancelou

        // Relacionamentos
        [ForeignKey("RegistroHospedeID")]
        public virtual RegistroHospede RegistroHospede { get; set; } = null!;

        [ForeignKey("ProdutoID")]
        public virtual Produto Produto { get; set; } = null!;

        // Métodos de negócio
        public void CalcularTotal()
        {
            ValorTotal = Quantidade * ValorUnitario;
        }

        public bool IsAtivo() => Status.Equals("Ativo", StringComparison.OrdinalIgnoreCase);

        public bool IsCancelado() => Status.Equals("Cancelado", StringComparison.OrdinalIgnoreCase);

        public bool PodeCancelar(string codigoUsuario)
        {
            // Só pode cancelar se estiver ativo e for Recepção ou Supervisor
            // Implementação da regra de negócio será feita no service
            return IsAtivo();
        }

        public void Cancelar(string motivo, string usuarioCancelamento)
        {
            Status = "Cancelado";
            ObservacoesCancelamento = motivo;
            DataCancelamento = DateTime.Now;
            UsuarioCancelamento = usuarioCancelamento;
        }

        public bool IsRecente()
        {
            // Considera recente se foi lançado nas últimas 2 horas
            return DateTime.Now.Subtract(DataHoraLancamento).TotalHours <= 2;
        }

        public string FormatarValorTotal() => ValorTotal.ToString("C2");

        public string FormatarDataHora() => DataHoraLancamento.ToString("dd/MM/yyyy HH:mm");

        public string FormatarQuantidade() => Quantidade % 1 == 0 ?
            Quantidade.ToString("0") :
            Quantidade.ToString("0.00");
    }
}