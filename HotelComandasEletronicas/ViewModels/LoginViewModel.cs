using System.ComponentModel.DataAnnotations;

namespace HotelComandasEletronicas.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Login é obrigatório")]
        [StringLength(50, ErrorMessage = "Login deve ter no máximo 50 caracteres")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Senha é obrigatória")]
        [StringLength(100, ErrorMessage = "Senha deve ter no máximo 100 caracteres")]
        [DataType(DataType.Password)]
        public string Senha { get; set; } = string.Empty;

        public bool LembrarMe { get; set; }

        public string? ReturnUrl { get; set; }

        // Propriedades auxiliares para a interface
        public string MensagemErro { get; set; } = string.Empty;
        public bool ExibirErro => !string.IsNullOrWhiteSpace(MensagemErro);

        // Métodos de validação
        public bool IsValido()
        {
            return !string.IsNullOrWhiteSpace(Login) &&
                   !string.IsNullOrWhiteSpace(Senha) &&
                   Login.Length <= 50 &&
                   Senha.Length <= 100;
        }

        public void LimparDados()
        {
            Login = string.Empty;
            Senha = string.Empty;
            LembrarMe = false;
            MensagemErro = string.Empty;
        }
    }
}