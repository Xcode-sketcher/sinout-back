// --- EXCEÇÃO PERSONALIZADA: O SINO DE ALARME ---
// Analogia simples: Como um "sino de alarme" na cozinha!
// Quando algo dá errado (ingrediente estragado, fogão apagado),
// tocamos o sino para alertar todo mundo sobre o problema.

namespace APISinout.Helpers;

public class AppException : Exception
{
    // Construtor: Como configurar o alarme com a mensagem
    public AppException(string message) : base(message) { }
}