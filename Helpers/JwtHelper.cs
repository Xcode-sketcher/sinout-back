using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using APISinout.Models;
using System.Text;

namespace APISinout.Helpers;

// Classe auxiliar para operações com JWT.
// Responsável por gerar tokens de autenticação.
public static class JwtHelper
{
    // Gera um token JWT para o usuário.
    public static string GenerateToken(User user, IConfiguration config)
    {
        if (string.IsNullOrEmpty(user.Email))
            throw new ArgumentException("Email do usuário não pode ser null ou vazio");

        if (string.IsNullOrEmpty(user.Role))
            throw new ArgumentException("Role do usuário não pode ser null ou vazio");

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("userId", user.Id ?? string.Empty),
            new Claim(ClaimTypes.NameIdentifier, user.Id ?? string.Empty),
            new Claim(ClaimTypes.Role, user.Role)
        };

        claims.Add(new Claim("email", user.Email));
        claims.Add(new Claim("role", user.Role));

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(config["Jwt:Key"]!));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}