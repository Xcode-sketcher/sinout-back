// --- MODELOS DE AUTENTICAÇÃO: OS INGREDIENTES BÁSICOS ---
// Analogia da cozinha: Estes são os "ingredientes básicos" para as receitas de autenticação!
// Como farinha, ovos e leite são essenciais para fazer um bolo, estes modelos são essenciais
// para registrar usuários e fazer login.

namespace APISinout.Models;

// Ingrediente: Pedido de registro (como uma lista de compras para o chef)
public class RegisterRequest
{
    public string? Name { get; set; } // Nome do ingrediente principal
    public string? Email { get; set; } // Código de barras
    public string? Password { get; set; } // Selo de qualidade
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
    public int Id { get; set; } // Número do prato
    public string? Name { get; set; } // Nome do prato
    public string? Email { get; set; } // Ingrediente principal
    public DateTime DataCadastro { get; set; } // Data de produção
    public bool Status { get; set; } // Se está fresco
    public string? Role { get; set; } // Tipo de prato (entrada, principal, sobremesa)

    // Construtor: Como montar o prato para apresentação
    public UserResponse(User user)
    {
        Id = user.Id;
        Name = user.Name;
        Email = user.Email;
        DataCadastro = user.DataCadastro;
        Status = user.Status;
        Role = user.Role;
    }
}