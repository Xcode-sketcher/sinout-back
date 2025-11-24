using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using APISinout.Services;
using APISinout.Data;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace APISinout.Tests.Integration;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:CookieSecure", "false" }
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remover registro existente de IEmailService se presente
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (descriptor != null)
                services.Remove(descriptor);

            // Adicionar implementação Noop que não faz nada (e registra logs) para evitar envio de emails durante testes
            services.AddSingleton<IEmailService, NoopEmailService>();

            // Remover MongoDbContext e Repositories para evitar conexão com banco de dados real
            RemoveService<MongoDbContext>(services);
            RemoveService<IUserRepository>(services);
            RemoveService<IPatientRepository>(services);
            RemoveService<IEmotionMappingRepository>(services);
            RemoveService<IHistoryRepository>(services);
            RemoveService<IPasswordResetRepository>(services);

            // Adicionar Repositories em memória
            services.AddSingleton<IUserRepository, InMemoryUserRepository>();
            services.AddSingleton<IPatientRepository, InMemoryPatientRepository>();
            services.AddSingleton<IEmotionMappingRepository, InMemoryEmotionMappingRepository>();
            services.AddSingleton<IHistoryRepository, InMemoryHistoryRepository>();
            services.AddSingleton<IPasswordResetRepository, InMemoryPasswordResetRepository>();
        });
    }

    private void RemoveService<T>(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null)
            services.Remove(descriptor);
    }

    public HttpClient CreateClientWithCookies()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
    }
}

public class NoopEmailService : IEmailService
{
    private readonly ILogger<NoopEmailService> _logger;
    public NoopEmailService(ILogger<NoopEmailService> logger) => _logger = logger;

    public Task SendPasswordResetEmailAsync(string toEmail, string resetCode)
    {
        _logger.LogInformation("[NoopEmailService] Skipping SendPasswordResetEmailAsync to {Email}", toEmail);
        return Task.CompletedTask;
    }

    public Task SendPasswordChangedNotificationAsync(string toEmail)
    {
        _logger.LogInformation("[NoopEmailService] Skipping SendPasswordChangedNotificationAsync to {Email}", toEmail);
        return Task.CompletedTask;
    }
}
