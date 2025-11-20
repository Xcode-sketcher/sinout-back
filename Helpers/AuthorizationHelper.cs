using System.Security.Claims;
using APISinout.Models;

namespace APISinout.Helpers;

// Classe auxiliar para operações de autorização.
// Fornece métodos para extrair informações do usuário autenticado.
public static class AuthorizationHelper
{
    // Obtém o ID do usuário atual a partir dos claims.
    public static int GetCurrentUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst("userId");

        if (userIdClaim == null)
        {
            userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        }

        if (userIdClaim == null)
        {
            throw new AppException("Usuário não encontrado");
        }

        if (!int.TryParse(userIdClaim.Value, out int userId))
        {
            throw new AppException($"Usuário não autenticado - userId inválido: {userIdClaim.Value}");
        }

        return userId;
    }

    // Obtém o papel do usuário atual a partir dos claims.
    public static string GetCurrentUserRole(ClaimsPrincipal user)
    {
        var roleClaim = user.FindFirst(ClaimTypes.Role);
        if (roleClaim == null)
            throw new AppException("Role não encontrada");

        return roleClaim.Value;
    }

    // Obtém o email do usuário atual a partir dos claims.
    public static string? GetCurrentUserEmail(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value;
    }

    // Verifica se o usuário atual é um administrador.
    public static bool IsAdmin(ClaimsPrincipal user)
    {
        return GetCurrentUserRole(user) == UserRole.Admin.ToString();
    }

    // Verifica se o usuário atual é um cuidador.
    public static bool IsCuidador(ClaimsPrincipal user)
    {
        return GetCurrentUserRole(user) == UserRole.Cuidador.ToString();
    }
}
