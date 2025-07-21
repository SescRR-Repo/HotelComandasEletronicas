using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelComandasEletronicas.Models
{
    [Table("PRODUTOS")]
    public class Produto
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [StringLength(100)]
        public string Descricao { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Valor { get; set; }

        [Required]
        [StringLength(30)]
        public string Categoria { get; set; } = string.Empty; // "Bebidas", "Comidas", "Serviços"

        [Required]
        public bool Status { get; set; } = true; // 1=Ativo, 0=Inativo

        [Required]
        public DateTime DataCadastro { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string UsuarioCadastro { get; set; } = string.Empty; // Login de quem cadastrou

        // Relacionamentos
        public virtual ICollection<LancamentoConsumo> Lancamentos { get; set; } = new List<LancamentoConsumo>();

        // Métodos de negócio
        public bool IsAtivo() => Status;

        public void Ativar() => Status = true;

        public void Inativar() => Status = false;

        public void AlterarPreco(decimal novoValor)
        {
            if (novoValor > 0)
                Valor = novoValor;
        }

        public bool IsCategoria(string categoria) =>
            Categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase);

        public string FormatarValor() => Valor.ToString("C2");

        public bool IsBebida() => IsCategoria("Bebidas");

        public bool IsComida() => IsCategoria("Comidas");

        public bool IsServico() => IsCategoria("Serviços");
    }
}