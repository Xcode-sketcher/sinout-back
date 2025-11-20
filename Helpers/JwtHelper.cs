// --- AJUDANTE JWT: O MAGO DAS SENHAS ---
// Analogia mista: Como um "mago" em um jogo que lança feitiços (tokens)!
// O JwtHelper é o assistente mágico que cria os "feitiços de proteção" (tokens JWT)
// para proteger o castelo. Ele assina e valida as credenciais dos jogadores.

using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using APISinout.Models;
using System.Text;

namespace APISinout.Helpers;

public static class JwtHelper
{
    // Feitiço: Gerar um token mágico para o usuário
    public static string GenerateToken(User user, IConfiguration config)
    {
        // Verificar se campos obrigatórios não são null
        if (string.IsNullOrEmpty(user.Email))
            throw new ArgumentException("Email do usuário não pode ser null ou vazio");
        
        if (string.IsNullOrEmpty(user.Role))
            throw new ArgumentException("Role do usuário não pode ser null ou vazio");


        // Os "ingredientes" do feitiço: informações do usuário
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email), // Assunto principal
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // ID único do feitiço
            new Claim("userId", user.UserId.ToString()), // ID numérico do usuário
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), // ID padrão .NET
            new Claim(ClaimTypes.Role, user.Role) // Cargo/poder do jogador
        };

        claims.Add(new Claim("email", user.Email));
        claims.Add(new Claim("role", user.Role));

        // A "varinha mágica": chave secreta para assinar
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        // Criar o feitiço (token)
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"], // Quem lançou o feitiço
            audience: config["Jwt:Audience"], // Para quem serve
            claims: claims, // Os poderes
            expires: DateTime.Now.AddHours(1), // Quanto tempo dura
            signingCredentials: creds // Assinatura
        );

        // Retornar o feitiço pronto
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}