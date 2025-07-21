using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelComandasEletronicas.Models
{
    [Table("LOGS_SISTEMA")]
    public class LogSistema
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public DateTime DataHora { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string CodigoUsuario { get; set; } = string.Empty; // Código ou login do usuário

        [Required]
        [StringLength(50)]
        public string Acao { get; set; } = string.Empty; // "Login", "Registro", "Lançamento", "Cancelamento"

        [Required]
        [StringLength(50)]
        public string Tabela { get; set; } = string.Empty; // Tabela afetada

        public int? RegistroID { get; set; } // ID do registro afetado

        [Column(TypeName = "nvarchar(max)")]
        public string? DetalhesAntes { get; set; } // Estado anterior (JSON)

        [Column(TypeName = "nvarchar(max)")]
        public string? DetalhesDepois { get; set; } // Estado posterior (JSON)

        [StringLength(45)]
        public string? IPAddress { get; set; }

        // Métodos de negócio
        public void RegistrarAcao(string codigoUsuario, string acao, string tabela, int? registroID = null,
            string? detalhesAntes = null, string? detalhesDepois = null, string? ipAddress = null)
        {
            CodigoUsuario = codigoUsuario;
            Acao = acao;
            Tabela = tabela;
            RegistroID = registroID;
            DetalhesAntes = detalhesAntes;
            DetalhesDepois = detalhesDepois;
            IPAddress = ipAddress;
            DataHora = DateTime.Now;
        }

        public bool IsAcaoSensivel()
        {
            string[] acoesSensiveis = { "Cancelamento", "Login", "Cadastro", "Alteracao" };
            return acoesSensiveis.Contains(Acao, StringComparer.OrdinalIgnoreCase);
        }

        public string FormatarDataHora() => DataHora.ToString("dd/MM/yyyy HH:mm:ss");

        public bool IsLogin() => Acao.Equals("Login", StringComparison.OrdinalIgnoreCase);

        public bool IsLancamento() => Acao.Equals("Lançamento", StringComparison.OrdinalIgnoreCase);

        public bool IsCancelamento() => Acao.Equals("Cancelamento", StringComparison.OrdinalIgnoreCase);

        public bool IsRegistro() => Acao.Equals("Registro", StringComparison.OrdinalIgnoreCase);
    }
}