// --- MODELOS DE PEDIDOS: AS RECEITAS ESTRUTURADAS ---
// Continuando na cozinha!
// Estes são os "formulários de pedido" que os clientes preenchem.
// Como uma receita estruturada: "quero um bolo com estes ingredientes específicos".

namespace APISinout.Models;

// Receita: Criar um novo usuário (como encomendar um prato personalizado)
public class CreateUserRequest
{
    public string? Name { get; set; } // Nome do prato
    public string? Email { get; set; } // Ingrediente principal
    public string? Password { get; set; } // Tempero secreto
    public string? Role { get; set; } // Tipo de prato (opcional)
}

// Receita: Atualizar um usuário existente (como modificar uma receita)
public class UpdateUserRequest
{
    public string? Name { get; set; } // Mudar o nome
    public string? Email { get; set; } // Trocar ingrediente
    public bool? Status { get; set; } // Ativar/desativar (como congelar/descongelar)
    public string? Role { get; set; } // Mudar tipo de prato
}

// Receita: Registrar emoção do cuidador (vinda da API de Processamento)
public class CaregiverEmotionRequest
{
    public int CaregiverId { get; set; } // ID do cuidador (userId)
    public string? PatientName { get; set; } // Nome do paciente (campo de texto)
    public DateTime? Timestamp { get; set; } // Quando a emoção foi detectada
    public Dictionary<string, double>? EmotionsDetected { get; set; } // Todas as emoções detectadas
    public string? DominantEmotion { get; set; } // Emoção dominante
}

// Receita: Atualizar nome do paciente
public class UpdatePatientNameRequest
{
    public string PatientName { get; set; } = string.Empty; // Nome do paciente
}