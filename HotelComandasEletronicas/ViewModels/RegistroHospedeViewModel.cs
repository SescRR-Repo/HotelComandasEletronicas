using System.ComponentModel.DataAnnotations;
using HotelComandasEletronicas.Models;

namespace HotelComandasEletronicas.ViewModels
{
    /// <summary>
    /// ViewModel para registro de hóspedes
    /// </summary>
    public class RegistroHospedeViewModel
    {
        public int ID { get; set; }

        [Required(ErrorMessage = "Número do quarto é obrigatório")]
        [StringLength(20, ErrorMessage = "Número do quarto deve ter no máximo 20 caracteres")]
        [Display(Name = "Número do Quarto")]
        public string NumeroQuarto { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nome do cliente é obrigatório")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Nome deve ter entre 2 e 100 caracteres")]
        [Display(Name = "Nome do Cliente")]
        public string NomeCliente { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefone é obrigatório")]
        [StringLength(20, ErrorMessage = "Telefone deve ter no máximo 20 caracteres")]
        [Display(Name = "Telefone do Cliente")]
        [Phone(ErrorMessage = "Formato de telefone inválido")]
        public string TelefoneCliente { get; set; } = string.Empty;

        // Propriedades somente leitura (para edição/visualização)
        public DateTime DataRegistro { get; set; }
        public decimal ValorGastoTotal { get; set; }
        public string Status { get; set; } = "Ativo";
        public string? UsuarioRegistro { get; set; }

        // Propriedades auxiliares para interface
        public bool IsAtivo => Status == "Ativo";
        public bool PodeFinalizar { get; set; }
        public bool TemConsumosAtivos { get; set; }

        // Métodos de validação
        public bool IsValido()
        {
            return !string.IsNullOrWhiteSpace(NumeroQuarto) &&
                   !string.IsNullOrWhiteSpace(NomeCliente) &&
                   !string.IsNullOrWhiteSpace(TelefoneCliente) &&
                   NumeroQuarto.Length <= 20 &&
                   NomeCliente.Length >= 2 &&
                   NomeCliente.Length <= 100 &&
                   TelefoneCliente.Length <= 20;
        }

        public void LimparDados()
        {
            NumeroQuarto = string.Empty;
            NomeCliente = string.Empty;
            TelefoneCliente = string.Empty;
            Status = "Ativo";
            ValorGastoTotal = 0;
        }

        // Métodos auxiliares
        public string ObterResumo()
        {
            return $"Quarto {NumeroQuarto} - {NomeCliente}";
        }

        public string ObterInformacaoCompleta()
        {
            return $"Quarto {NumeroQuarto} - {NomeCliente} - {TelefoneCliente} - {ValorGastoTotal:C}";
        }

        public string FormatarTelefone()
        {
            if (string.IsNullOrWhiteSpace(TelefoneCliente))
                return string.Empty;

            // Remove todos os caracteres não numéricos
            var numeros = new string(TelefoneCliente.Where(char.IsDigit).ToArray());

            // Aplica formatação baseada no tamanho
            return numeros.Length switch
            {
                11 => $"({numeros[..2]}) {numeros[2..7]}-{numeros[7..]}",
                10 => $"({numeros[..2]}) {numeros[2..6]}-{numeros[6..]}",
                _ => TelefoneCliente
            };
        }

        // Conversão para Model
        public RegistroHospede ParaModel()
        {
            return new RegistroHospede
            {
                ID = this.ID,
                NumeroQuarto = this.NumeroQuarto.Trim(),
                NomeCliente = this.NomeCliente.Trim(),
                TelefoneCliente = this.TelefoneCliente.Trim(),
                DataRegistro = this.ID == 0 ? DateTime.Now : this.DataRegistro,
                ValorGastoTotal = this.ValorGastoTotal,
                Status = this.Status,
                UsuarioRegistro = this.UsuarioRegistro ?? string.Empty
            };
        }

        // Conversão de Model
        public static RegistroHospedeViewModel DeModel(RegistroHospede registro)
        {
            return new RegistroHospedeViewModel
            {
                ID = registro.ID,
                NumeroQuarto = registro.NumeroQuarto,
                NomeCliente = registro.NomeCliente,
                TelefoneCliente = registro.TelefoneCliente,
                DataRegistro = registro.DataRegistro,
                ValorGastoTotal = registro.ValorGastoTotal,
                Status = registro.Status,
                UsuarioRegistro = registro.UsuarioRegistro
            };
        }

        // Métodos de validação específicos
        public static bool ValidarNumeroQuarto(string numeroQuarto)
        {
            if (string.IsNullOrWhiteSpace(numeroQuarto))
                return false;

            // Permitir números, letras e alguns caracteres especiais
            return numeroQuarto.All(c => char.IsLetterOrDigit(c) || c == '-' || c == ' ');
        }

        public static bool ValidarTelefone(string telefone)
        {
            if (string.IsNullOrWhiteSpace(telefone))
                return false;

            // Remove formatação e verifica se tem pelo menos 8 dígitos
            var numeros = new string(telefone.Where(char.IsDigit).ToArray());
            return numeros.Length >= 8 && numeros.Length <= 15;
        }

        public static string FormatarTelefoneEstatico(string telefone)
        {
            if (string.IsNullOrWhiteSpace(telefone))
                return string.Empty;

            var numeros = new string(telefone.Where(char.IsDigit).ToArray());

            return numeros.Length switch
            {
                11 => $"({numeros[..2]}) {numeros[2..7]}-{numeros[7..]}",
                10 => $"({numeros[..2]}) {numeros[2..6]}-{numeros[6..]}",
                _ => telefone
            };
        }
    }

    /// <summary>
    /// ViewModel para busca de hóspedes
    /// </summary>
    public class BuscaHospedeViewModel
    {
        public string? Termo { get; set; }
        public string? Status { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }

        public List<RegistroHospede> Resultados { get; set; } = new();

        // Propriedades calculadas
        public int TotalResultados => Resultados.Count;
        public int HospedesAtivos => Resultados.Count(r => r.IsAtivo());
        public int HospedesFinalizados => Resultados.Count(r => !r.IsAtivo());
        public decimal ValorTotalGeral => Resultados.Sum(r => r.ValorGastoTotal);

        // Métodos auxiliares
        public bool TemResultados() => Resultados.Any();

        public bool TemFiltros()
        {
            return !string.IsNullOrWhiteSpace(Termo) ||
                   !string.IsNullOrWhiteSpace(Status) ||
                   DataInicio.HasValue ||
                   DataFim.HasValue;
        }

        public string ObterResumoFiltros()
        {
            var filtros = new List<string>();

            if (!string.IsNullOrWhiteSpace(Termo))
                filtros.Add($"Termo: {Termo}");

            if (!string.IsNullOrWhiteSpace(Status))
                filtros.Add($"Status: {Status}");

            if (DataInicio.HasValue)
                filtros.Add($"De: {DataInicio.Value:dd/MM/yyyy}");

            if (DataFim.HasValue)
                filtros.Add($"Até: {DataFim.Value:dd/MM/yyyy}");

            return filtros.Any() ? string.Join(", ", filtros) : "Sem filtros";
        }

        public Dictionary<string, int> ObterEstatisticasPorStatus()
        {
            return Resultados
                .GroupBy(r => r.Status)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }

    /// <summary>
    /// ViewModel para finalização de registro
    /// </summary>
    public class FinalizacaoRegistroViewModel
    {
        [Required(ErrorMessage = "ID do registro é obrigatório")]
        public int RegistroID { get; set; }

        [StringLength(200, ErrorMessage = "Observações devem ter no máximo 200 caracteres")]
        public string? Observacoes { get; set; }

        public string? UsuarioFinalizacao { get; set; }

        // Propriedades informativas
        public string? NumeroQuarto { get; set; }
        public string? NomeCliente { get; set; }
        public decimal ValorTotal { get; set; }
        public DateTime DataRegistro { get; set; }

        // Métodos
        public bool IsValido()
        {
            return RegistroID > 0;
        }

        public string ObterResumoFinalizacao()
        {
            return $"Quarto {NumeroQuarto} - {NomeCliente}: {ValorTotal:C} ({DataRegistro:dd/MM/yyyy})";
        }
    }
}