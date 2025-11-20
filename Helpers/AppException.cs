namespace APISinout.Helpers;

// Exceção personalizada para a aplicação.
// Usada para lançar erros específicos do domínio.
public class AppException : Exception
{
    // Construtor que inicializa a exceção com uma mensagem.
    public AppException(string message) : base(message) { }
}