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

    // ID do usuário.
    public string? UserId { get; set; }

    // Nome do usuário.
    public string? Name { get; set; }

    // Email do usuário.
    public string Email { get; set; } = string.Empty;

    // Papel do usuário: Admin ou Cuidador.
    public string? Role { get; set; }

    // Telefone do usuário.
    public string? Phone { get; set; }

    // Data do último login.
    public DateTime? LastLogin { get; set; }

    // ID do paciente associado.
    public string? PatientId { get; set; }

    // Nome do paciente associado.
    public string? PatientName { get; set; }

    // Construtor que inicializa a partir de um objeto User.
    public UserResponse(User user, string? patientId = null, string? patientName = null)
    {
        UserId = user.Id;
        Name = user.Name;
        Email = user.Email ?? string.Empty;
        Role = user.Role;
        Phone = user.Phone;
        LastLogin = user.LastLogin;
        PatientId = patientId;
        PatientName = patientName;
    }
}

// Representa uma solicitação para reenviar código de reset de senha.
public class ResendResetCodeRequest
{
    // Email do usuário.
    public string Email { get; set; } = string.Empty;
}