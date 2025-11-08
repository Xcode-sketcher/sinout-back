namespace APISinout.Models;

public class RegisterRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public class LoginRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public class AuthResponse
{
    public UserResponse? User { get; set; }
    public string? Token { get; set; }
}

public class UserResponse
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public DateTime DataCadastro { get; set; }
    public bool Status { get; set; }
    public string? Role { get; set; }

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