using System.ComponentModel.DataAnnotations;
using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.ViewModels
{
    public class UsuarioViewModel
    {
        public int ID { get; set; }

        [Required(ErrorMessage = "Nome é obrigatório")]
        [StringLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Login é obrigatório")]
        [StringLength(50, ErrorMessage = "Login deve ter no máximo 50 caracteres")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Código ID é obrigatório")]
        [StringLength(2, MinimumLength = 2, ErrorMessage = "Código ID deve ter exatamente 2 dígitos")]
        [RegularExpression(@"^\d{2}$", ErrorMessage = "Código ID deve conter apenas números")]
        public string CodigoID { get; set; } = string.Empty;

        [Required(ErrorMessage = "Perfil é obrigatório")]
        public string Perfil { get; set; } = string.Empty;

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Senha deve ter entre 6 e 100 caracteres")]
        [DataType(DataType.Password)]
        public string? Senha { get; set; }

        [DataType(DataType.Password)]
        [Compare("Senha", ErrorMessage = "Confirmação de senha não confere")]
        public string? ConfirmarSenha { get; set; }

        public bool Status { get; set; } = true;

        public DateTime DataCadastro { get; set; }

        public DateTime? UltimoAcesso { get; set; }

        // Propriedades auxiliares
        public bool IsEdicao => ID > 0;
        public bool RequererSenha => Perfil == "Recepção" || Perfil == "Supervisor";

        // Lista de perfis disponíveis
        public static List<string> PerfisDisponiveis => new List<string>
        {
            "Garçom",
            "Recepção",
            "Supervisor"
        };

        // Métodos de conversão
        public Usuario ParaModel()
        {
            return new Usuario
            {
                ID = this.ID,
                Nome = this.Nome,
                Login = this.Login,
                CodigoID = this.CodigoID,
                Perfil = this.Perfil,
                Senha = this.Senha ?? string.Empty,
                Status = this.Status,
                DataCadastro = this.DataCadastro
            };
        }

        public static UsuarioViewModel DeModel(Usuario usuario)
        {
            return new UsuarioViewModel
            {
                ID = usuario.ID,
                Nome = usuario.Nome,
                Login = usuario.Login,
                CodigoID = usuario.CodigoID,
                Perfil = usuario.Perfil,
                Status = usuario.Status,
                DataCadastro = usuario.DataCadastro,
                UltimoAcesso = usuario.UltimoAcesso
            };
        }

        // Validações customizadas
        public bool IsValido(out List<string> erros)
        {
            erros = new List<string>();

            if (string.IsNullOrWhiteSpace(Nome))
                erros.Add("Nome é obrigatório");

            if (string.IsNullOrWhiteSpace(Login))
                erros.Add("Login é obrigatório");

            if (string.IsNullOrWhiteSpace(CodigoID) || CodigoID.Length != 2 || !CodigoID.All(char.IsDigit))
                erros.Add("Código ID deve ter exatamente 2 dígitos");

            if (string.IsNullOrWhiteSpace(Perfil))
                erros.Add("Perfil é obrigatório");

            if (RequererSenha && !IsEdicao && string.IsNullOrWhiteSpace(Senha))
                erros.Add("Senha é obrigatória para perfis Recepção e Supervisor");

            if (!string.IsNullOrWhiteSpace(Senha) && Senha.Length < 6)
                erros.Add("Senha deve ter pelo menos 6 caracteres");

            if (Senha != ConfirmarSenha)
                erros.Add("Confirmação de senha não confere");

            return !erros.Any();
        }

        public string ObterClassePerfilBadge()
        {
            return Perfil switch
            {
                "Garçom" => "bg-success",
                "Recepção" => "bg-primary",
                "Supervisor" => "bg-warning text-dark",
                _ => "bg-secondary"
            };
        }

        public string ObterIconePerfil()
        {
            return Perfil switch
            {
                "Garçom" => "fas fa-cocktail",
                "Recepção" => "fas fa-desk",
                "Supervisor" => "fas fa-crown",
                _ => "fas fa-user"
            };
        }
    }

    // ViewModel para validação de código
    public class ValidarCodigoViewModel
    {
        [Required(ErrorMessage = "Código é obrigatório")]
        [StringLength(2, MinimumLength = 2, ErrorMessage = "Código deve ter exatamente 2 dígitos")]
        [RegularExpression(@"^\d{2}$", ErrorMessage = "Código deve conter apenas números")]
        public string Codigo { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }

        public string MensagemErro { get; set; } = string.Empty;
        public bool ExibirErro => !string.IsNullOrWhiteSpace(MensagemErro);

        public bool IsValido()
        {
            return !string.IsNullOrWhiteSpace(Codigo) &&
                   Codigo.Length == 2 &&
                   Codigo.All(char.IsDigit);
        }
    }
}