namespace APISinout.Models;

// Representa uma solicitação de registro de usuário.
public class RegisterRequest
{
    // Nome do usuário.
    public string? Name { get; set; }

    // Email único do usuário.
    public string? Email { get; set; }

    // Senha do usuário.
    public string? Password { get; set; }

    // Telefone do usuário (opcional).
    public string? Phone { get; set; }

    // Nome do paciente associado (opcional).
    public string? PatientName { get; set; }

    // Papel do usuário: Admin ou Cuidador (padrão: Cuidador).
    public string? Role { get; set; }
}

// Representa uma solicitação de login.
public class LoginRequest
{
    // Email do usuário para identificação.
    public string? Email { get; set; }

    // Senha do usuário.
    public string? Password { get; set; }
}

// Representa a resposta de autenticação.
public class AuthResponse
{
    // Dados do usuário autenticado.
    public UserResponse? User { get; set; }

    // Token de autenticação JWT.
    public string? Token { get; set; }
}

// Representa a resposta com dados do usuário.
public class UserResponse
{
    // Construtor padrão para desserialização JSON.
    public UserResponse() { }

    // ID numérico do usuário.
    public int UserId { get; set; }

    // Nome do usuário.
    public string? Name { get; set; }

    // Email do usuário.
    public string? Email { get; set; }

    // Data de cadastro do usuário.
    public DateTime DataCadastro { get; set; }

    // Indica se o usuário está ativo.
    public bool Status { get; set; }

    // Papel do usuário: Admin ou Cuidador.
    public string? Role { get; set; }

    // Telefone do usuário.
    public string? Phone { get; set; }

    // Data do último login.
    public DateTime? LastLogin { get; set; }

    // Construtor que inicializa a partir de um objeto User.
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

// Representa uma solicitação para reenviar código de reset de senha.
public class ResendResetCodeRequest
{
    // Email do usuário.
    public string Email { get; set; } = string.Empty;
}