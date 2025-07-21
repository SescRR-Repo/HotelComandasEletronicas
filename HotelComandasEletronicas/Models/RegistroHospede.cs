using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelComandasEletronicas.Models
{
    [Table("REGISTROS_HOSPEDE")]
    public class RegistroHospede
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [StringLength(20)]
        public string NumeroQuarto { get; set; } = string.Empty; // "101", "205A", "Chalé 3"

        [Required]
        [StringLength(100)]
        public string NomeCliente { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string TelefoneCliente { get; set; } = string.Empty;

        [Required]
        public DateTime DataRegistro { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal ValorGastoTotal { get; set; } = 0.00m; // Soma automática dos consumos

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Ativo"; // "Ativo", "Finalizado"

        [Required]
        [StringLength(50)]
        public string UsuarioRegistro { get; set; } = string.Empty; // Login de quem registrou

        // Relacionamentos
        public virtual ICollection<LancamentoConsumo> Lancamentos { get; set; } = new List<LancamentoConsumo>();

        // Métodos de negócio
        public bool IsAtivo() => Status.Equals("Ativo", StringComparison.OrdinalIgnoreCase);

        public bool PodeFinalizar() => IsAtivo() && ValorGastoTotal >= 0;

        public void Finalizar()
        {
            Status = "Finalizado";
        }

        public void CalcularTotalGasto()
        {
            ValorGastoTotal = Lancamentos
                .Where(l => l.Status.Equals("Ativo", StringComparison.OrdinalIgnoreCase))
                .Sum(l => l.ValorTotal);
        }

        public int TotalItensConsumidos() => Lancamentos
            .Where(l => l.Status.Equals("Ativo", StringComparison.OrdinalIgnoreCase))
            .Count();

        public DateTime? UltimoConsumo() => Lancamentos
            .Where(l => l.Status.Equals("Ativo", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(l => l.DataHoraLancamento)
            .FirstOrDefault()?.DataHoraLancamento;
    }
}