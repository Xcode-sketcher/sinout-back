namespace APISinout.Helpers;

public class AppException : Exception
{
    public AppException(string message) : base(message) { }
}