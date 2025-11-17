using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using APISinout.Services;

namespace APISinout.Tests.Integration;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing IEmailService registration if present
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add a Noop implementation that does nothing (and logs) to avoid sending emails during tests
            services.AddSingleton<IEmailService, NoopEmailService>();
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
