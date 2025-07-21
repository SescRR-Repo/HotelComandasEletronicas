using System.ComponentModel.DataAnnotations;

namespace HotelComandasEletronicas.ViewModels
{
    /// <summary>
    /// ViewModel para validação de código de usuário (principalmente para garçons)
    /// </summary>
    public class ValidarCodigoViewModel
    {
        [Required(ErrorMessage = "Código é obrigatório")]
        [StringLength(2, MinimumLength = 2, ErrorMessage = "Código deve ter exatamente 2 dígitos")]
        [RegularExpression(@"^\d{2}$", ErrorMessage = "Código deve conter apenas números")]
        public string Codigo { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }

        public string MensagemErro { get; set; } = string.Empty;

        public bool ExibirErro => !string.IsNullOrWhiteSpace(MensagemErro);

        // Métodos de validação
        public bool IsValido()
        {
            return !string.IsNullOrWhiteSpace(Codigo) &&
                   Codigo.Length == 2 &&
                   Codigo.All(char.IsDigit);
        }

        public void LimparDados()
        {
            Codigo = string.Empty;
            MensagemErro = string.Empty;
        }

        // Métodos auxiliares
        public bool IsCodigoCompleto()
        {
            return Codigo.Length == 2;
        }

        public bool IsCodigoNumerico()
        {
            return Codigo.All(char.IsDigit);
        }

        public string ObterCodigoFormatado()
        {
            return Codigo.PadLeft(2, '0');
        }
    }
}