// --- HELPER DE AUTORIZAÇÃO: UTILITÁRIOS PARA SEGURANÇA ---
// Facilita a obtenção de informações do usuário autenticado

using System.Security.Claims;
using APISinout.Models;

namespace APISinout.Helpers;

public static class AuthorizationHelper
{
    public static int GetCurrentUserId(ClaimsPrincipal user)
    {
        // Primeiro tentar pegar claim customizado "userId"
        var userIdClaim = user.FindFirst("userId");
        
        // Fallback para ClaimTypes.NameIdentifier
        if (userIdClaim == null)
        {
            userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        }
        
        if (userIdClaim == null)
        {
            // Debug: mostrar todos os claims disponíveis
            var allClaims = string.Join(", ", user.Claims.Select(c => $"{c.Type}={c.Value}"));
            Console.WriteLine($"[DEBUG AUTH] Claims disponíveis: {allClaims}");
            throw new AppException("Usuário não autenticado - claim userId não encontrado");
        }
        
        if (!int.TryParse(userIdClaim.Value, out int userId))
        {
            throw new AppException($"Usuário não autenticado - userId inválido: {userIdClaim.Value}");
        }
        
        return userId;
    }

    public static string GetCurrentUserRole(ClaimsPrincipal user)
    {
        var roleClaim = user.FindFirst(ClaimTypes.Role);
        if (roleClaim == null)
            throw new AppException("Role não encontrada");
        
        return roleClaim.Value;
    }

    public static string? GetCurrentUserEmail(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value;
    }

    public static bool IsAdmin(ClaimsPrincipal user)
    {
        return GetCurrentUserRole(user) == UserRole.Admin.ToString();
    }

    public static bool IsCuidador(ClaimsPrincipal user)
    {
        return GetCurrentUserRole(user) == UserRole.Cuidador.ToString();
    }
}
