using APISinout.Models;

namespace APISinout.Tests.Fixtures;

public static class PasswordResetFixtures
{
    public static PasswordResetToken CreateValidToken(string? userId = null, string email = "test@test.com")
    {
        return new PasswordResetToken
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            UserId = userId ?? MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Email = email,
            Token = "123456",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            Used = false
        };
    }

    public static PasswordResetToken CreateExpiredToken(string? userId = null)
    {
        var token = CreateValidToken(userId);
        token.ExpiresAt = DateTime.UtcNow.AddHours(-1);
        return token;
    }

    public static PasswordResetToken CreateUsedToken(string? userId = null)
    {
        var token = CreateValidToken(userId);
        token.Used = true;
        return token;
    }

    public static ForgotPasswordRequest CreateForgotPasswordRequest(string email = "test@test.com")
    {
        return new ForgotPasswordRequest { Email = email };
    }

    public static ResetPasswordRequest CreateResetPasswordRequest(string token = "123456")
    {
        return new ResetPasswordRequest
        {
            Token = token,
            NewPassword = "NewPass@123",
            ConfirmPassword = "NewPass@123"
        };
    }

    public static ChangePasswordRequest CreateChangePasswordRequest()
    {
        return new ChangePasswordRequest
        {
            CurrentPassword = "Test@123",
            NewPassword = "NewPass@123",
            ConfirmPassword = "NewPass@123"
        };
    }
}
