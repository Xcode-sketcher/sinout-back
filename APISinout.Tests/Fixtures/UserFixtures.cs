using APISinout.Models;

namespace APISinout.Tests.Fixtures;

public static class UserFixtures
{
    public static User CreateValidUser(string? userId = null, string role = "Cuidador")
    {
        return new User
        {
            Id = userId ?? MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Name = "João Silva",
            Email = "joao.silva@test.com",
            Phone = "+55 11 99999-9999",
            DataCadastro = DateTime.UtcNow,
            Role = role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@123"),
            CreatedBy = "test",
            LastLogin = null
        };
    }

    public static User CreateAdminUser(string? userId = null)
    {
        return CreateValidUser(userId, "Admin");
    }

    public static RegisterRequest CreateValidRegisterRequest()
    {
        return new RegisterRequest
        {
            Name = "João Silva",
            Email = "joao.silva@test.com",
            Password = "Test@123",
            Phone = "+55 11 99999-9999",
            PatientName = "Maria Silva",
            Role = "Cuidador"
        };
    }

    public static LoginRequest CreateValidLoginRequest()
    {
        return new LoginRequest
        {
            Email = "joao.silva@test.com",
            Password = "Test@123"
        };
    }
}
