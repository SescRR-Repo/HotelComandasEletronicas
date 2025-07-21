using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelComandasEletronicas.Models
{
    [Table("USUARIOS_SISTEMA")]
    public class Usuario
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Login { get; set; } = string.Empty; // anacclara01, mariasilva01

        [Required]
        [StringLength(2)]
        public string CodigoID { get; set; } = string.Empty; // 01, 03, 18, etc

        [Required]
        [StringLength(20)]
        public string Perfil { get; set; } = string.Empty; // Garçom, Recepção, Supervisor

        [Required]
        [StringLength(255)]
        public string Senha { get; set; } = string.Empty; // Hash criptografado

        [Required]
        public bool Status { get; set; } = true; // 1=Ativo, 0=Inativo

        [Required]
        public DateTime DataCadastro { get; set; } = DateTime.Now;

        public DateTime? UltimoAcesso { get; set; }

        // Métodos de negócio
        public bool IsAtivo() => Status;

        public bool IsGarcom() => Perfil.Equals("Garçom", StringComparison.OrdinalIgnoreCase);

        public bool IsRecepcao() => Perfil.Equals("Recepção", StringComparison.OrdinalIgnoreCase);

        public bool IsSupervisor() => Perfil.Equals("Supervisor", StringComparison.OrdinalIgnoreCase);

        public bool TemPermissaoLogin() => IsRecepcao() || IsSupervisor();

        public bool TemPermissaoCancelamento() => IsRecepcao() || IsSupervisor();

        public bool TemPermissaoCadastro() => IsSupervisor();
    }
}