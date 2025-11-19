using APISinout.Models;

namespace APISinout.Tests.Fixtures;

public static class UserFixtures
{
    public static User CreateValidUser(int userId = 1, string role = "Cuidador")
    {
        return new User
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            UserId = userId,
            Name = "João Silva",
            Email = "joao.silva@test.com",
            Phone = "+55 11 99999-9999",
            PatientName = "Maria Silva",
            DataCadastro = DateTime.UtcNow,
            Status = true,
            Role = role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@123"),
            CreatedBy = "test",
            LastLogin = null
        };
    }

    public static User CreateAdminUser(int userId = 100)
    {
        return CreateValidUser(userId, "Admin");
    }

    public static User CreateInactiveUser(int userId = 2)
    {
        var user = CreateValidUser(userId);
        user.Status = false;
        return user;
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
