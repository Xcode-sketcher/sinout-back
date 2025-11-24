namespace APISinout.Models;

// Representa uma solicitação para criar um novo usuário.
public class CreateUserRequest
{
    // Nome do usuário.
    public string? Name { get; set; }

    // Email do usuário.
    public string? Email { get; set; }

    // Senha do usuário.
    public string? Password { get; set; }

    // Papel do usuário (opcional).
    public string? Role { get; set; }
}

// Representa uma solicitação para atualizar um usuário existente.
public class UpdateUserRequest
{
    // Nome do usuário.
    public string? Name { get; set; }

    // Email do usuário.
    public string? Email { get; set; }

    // Papel do usuário.
    public string? Role { get; set; }
}

// Representa uma solicitação de registro de emoção do cuidador.
// Vinda da API de processamento.
public class CuidadorEmotionRequest
{
    // ID do cuidador (userId).
    public int CuidadorId { get; set; }

    // Nome do paciente (campo de texto).
    public string? PatientName { get; set; }

    // Quando a emoção foi detectada.
    public DateTime? Timestamp { get; set; }

    // Todas as emoções detectadas com percentuais.
    public Dictionary<string, double>? EmotionsDetected { get; set; }

    // Emoção dominante.
    public string? DominantEmotion { get; set; }
}

// Representa uma solicitação para atualizar o nome do paciente.
public class UpdatePatientNameRequest
{
    // Nome do paciente.
    public string PatientName { get; set; } = string.Empty;
}