// --- MODELOS DE AUTENTICAÇÃO: OS INGREDIENTES BÁSICOS ---
// Analogia da cozinha: Estes são os "ingredientes básicos" para as receitas de autenticação!
// Como farinha, ovos e leite são essenciais para fazer um bolo, estes modelos são essenciais
// para registrar usuários e fazer login.

namespace APISinout.Models;

// Ingrediente: Pedido de registro (como uma lista de compras para o chef)
public class RegisterRequest
{
    public string? Name { get; set; } // Nome do usuário
    public string? Email { get; set; } // Email único
    public string? Password { get; set; } // Senha
    public string? Phone { get; set; } // Telefone (opcional)
    public string? PatientName { get; set; } // Nome do paciente (opcional)
    public string? Role { get; set; } // Admin ou Caregiver (default: Caregiver)
}

// Ingrediente: Pedido de login (como verificar se o ingrediente está bom)
public class LoginRequest
{
    public string? Email { get; set; } // Identificar o lote
    public string? Password { get; set; } // Verificar frescor
}

// Prato pronto: Resposta de autenticação (como servir o prato completo)
public class AuthResponse
{
    public UserResponse? User { get; set; } // O prato principal
    public string? Token { get; set; } // O certificado de qualidade
}

// Apresentação do prato: Resposta do usuário (como decorar o prato para servir)
public class UserResponse
{
    public int UserId { get; set; } // ID numérico do usuário
    public string? Name { get; set; } // Nome
    public string? Email { get; set; } // Email
    public DateTime DataCadastro { get; set; } // Data de cadastro
    public bool Status { get; set; } // Se está ativo
    public string? Role { get; set; } // Admin ou Caregiver
    public string? Phone { get; set; } // Telefone
    public DateTime? LastLogin { get; set; } // Último login

    // Construtor: Como montar o usuário para apresentação
    public UserResponse(User user)
    {
        UserId = user.UserId;
        Name = user.Name;
        Email = user.Email;
        DataCadastro = user.DataCadastro;
        Status = user.Status;
        Role = user.Role;
        Phone = user.Phone;
        LastLogin = user.LastLogin;
    }
}