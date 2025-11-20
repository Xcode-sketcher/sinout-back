using APISinout.Data;

namespace APISinout.Services;

// Serviço de limpeza automática de tokens expirados.
public class TokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenCleanupService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1); // Executa a cada 1 hora

    public TokenCleanupService(
        IServiceProvider serviceProvider,
        ILogger<TokenCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    // Executa a limpeza de tokens expirados periodicamente.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[TokenCleanup] Serviço de limpeza de tokens iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredTokensAsync();
                await Task.Delay(_interval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TokenCleanup] Erro ao limpar tokens expirados");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Retry em 5 minutos se houver erro
            }
        }
    }

    // Limpa os tokens expirados do banco de dados.
    private async Task CleanupExpiredTokensAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPasswordResetRepository>();

        _logger.LogInformation("[TokenCleanup] Iniciando limpeza de tokens expirados...");
        
        await repository.DeleteExpiredTokensAsync();
        
        _logger.LogInformation("[TokenCleanup] Limpeza concluída");
    }
}
