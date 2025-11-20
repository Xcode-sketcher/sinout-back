// --- SERVIÇO DE EMAIL ---
// Responsável por enviar emails de redefinição de senha

using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace APISinout.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string resetCode);
    Task SendPasswordChangedNotificationAsync(string toEmail);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Prioridade: Variáveis de Ambiente > appsettings.Development.json > appsettings.json
        _smtpServer = Environment.GetEnvironmentVariable("EMAIL__SMTPSERVER") 
            ?? _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(Environment.GetEnvironmentVariable("EMAIL__SMTPPORT") 
            ?? _configuration["Email:SmtpPort"] ?? "587");
        _smtpUsername = Environment.GetEnvironmentVariable("EMAIL__USERNAME") 
            ?? _configuration["Email:Username"] ?? "";
        _smtpPassword = Environment.GetEnvironmentVariable("EMAIL__PASSWORD") 
            ?? _configuration["Email:Password"] ?? "";
        _fromEmail = Environment.GetEnvironmentVariable("EMAIL__FROMEMAIL") 
            ?? _configuration["Email:FromEmail"] ?? "";
        _fromName = _configuration["Email:FromName"] ?? "Sinout - Sistema de Cuidados";

        _logger.LogInformation("[EmailService] Configurado - SMTP: {Server}:{Port}, User: {User}", 
            _smtpServer, _smtpPort, string.IsNullOrEmpty(_smtpUsername) ? "NÃO CONFIGURADO" : _smtpUsername);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetCode)
    {
        try
        {
            _logger.LogInformation("[EmailService] Preparando email de reset para {Email}", toEmail);

            // Validar configurações
            if (string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword))
            {
                _logger.LogWarning("[EmailService] MODO DEV: Credenciais de email não configuradas");
                _logger.LogInformation("[EmailService] Email que seria enviado para: {Email}", toEmail);
                _logger.LogInformation("[EmailService] Código de redefinição: {Code}", resetCode);
                return;
            }

            var subject = "Redefinição de Senha - Sinout";
            var body = GeneratePasswordResetEmailBody(resetCode);

            await SendEmailAsync(toEmail, subject, body);
            
            _logger.LogInformation("[EmailService] Email de reset enviado com sucesso para {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmailService] Erro ao enviar email de reset para {Email}", toEmail);
            throw new Exception($"Erro ao enviar email: {ex.Message}");
        }
    }

    public async Task SendPasswordChangedNotificationAsync(string toEmail)
    {
        try
        {
            _logger.LogInformation("[EmailService] Enviando notificação de senha alterada para {Email}", toEmail);

            // Validar configurações
            if (string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword))
            {
                _logger.LogWarning("[EmailService] MODO DEV: Email de notificação não enviado (credenciais não configuradas)");
                return;
            }

            var subject = "Senha Alterada com Sucesso - Sinout";
            var body = GeneratePasswordChangedEmailBody();

            await SendEmailAsync(toEmail, subject, body);
            
            _logger.LogInformation("[EmailService] Notificação de senha alterada enviada para {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmailService] Erro ao enviar notificação para {Email}", toEmail);
            // Não lançar exceção - notificação é opcional
        }
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        using var smtpClient = new SmtpClient(_smtpServer, _smtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
            Timeout = 10000 // 10 segundos
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_fromEmail, _fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        
        mailMessage.To.Add(toEmail);

        await smtpClient.SendMailAsync(mailMessage);
    }

    internal string GeneratePasswordResetEmailBody(string resetCode)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .container {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 30px;
            border-radius: 10px;
            text-align: center;
        }}
        .content {{
            background: white;
            padding: 30px;
            border-radius: 8px;
            margin-top: 20px;
        }}
        .code {{
            background: #f0f0f0;
            padding: 15px;
            border-radius: 5px;
            font-size: 28px;
            font-weight: bold;
            letter-spacing: 5px;
            color: #667eea;
            margin: 20px 0;
        }}
        .warning {{
            background: #fff3cd;
            border: 1px solid #ffc107;
            padding: 15px;
            border-radius: 5px;
            margin-top: 20px;
            color: #856404;
        }}
        h1 {{
            color: white;
            margin: 0;
        }}
        .footer {{
            margin-top: 30px;
            font-size: 12px;
            color: #666;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>🔐 Sinout - Redefinição de Senha</h1>
    </div>
    
    <div class='content'>
        <h2>Olá!</h2>
        <p>Recebemos uma solicitação para redefinir a senha da sua conta.</p>
        
        <p>Use o código abaixo para redefinir sua senha:</p>
        
        <div class='code'>{resetCode}</div>
        
        <p><strong>Este código expira em 1 hora.</strong></p>
        
        <div class='warning'>
            ⚠️ <strong>Importante:</strong><br>
            Se você não solicitou a redefinição de senha, ignore este email e sua senha permanecerá inalterada.
        </div>
        
        <div class='footer'>
            <p>Este é um email automático, por favor não responda.</p>
            <p>&copy; 2025 Sinout - Sistema de Cuidados</p>
        </div>
    </div>
</body>
</html>";
    }

    internal string GeneratePasswordChangedEmailBody()
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .container {{
            background: linear-gradient(135deg, #28a745 0%, #20c997 100%);
            padding: 30px;
            border-radius: 10px;
            text-align: center;
        }}
        .content {{
            background: white;
            padding: 30px;
            border-radius: 8px;
            margin-top: 20px;
        }}
        .success {{
            background: #d4edda;
            border: 1px solid #28a745;
            padding: 15px;
            border-radius: 5px;
            margin: 20px 0;
            color: #155724;
        }}
        .warning {{
            background: #fff3cd;
            border: 1px solid #ffc107;
            padding: 15px;
            border-radius: 5px;
            margin-top: 20px;
            color: #856404;
        }}
        h1 {{
            color: white;
            margin: 0;
        }}
        .footer {{
            margin-top: 30px;
            font-size: 12px;
            color: #666;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>✅ Senha Alterada com Sucesso</h1>
    </div>
    
    <div class='content'>
        <h2>Olá!</h2>
        
        <div class='success'>
            ✅ Sua senha foi alterada com sucesso em {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss} UTC
        </div>
        
        <p>Esta é uma notificação de segurança para informar que a senha da sua conta foi alterada.</p>
        
        <div class='warning'>
            ⚠️ <strong>Importante:</strong><br>
            Se você não realizou esta alteração, entre em contato conosco imediatamente.
        </div>
        
        <div class='footer'>
            <p>Este é um email automático, por favor não responda.</p>
            <p>&copy; 2025 Sinout - Sistema de Cuidados</p>
        </div>
    </div>
</body>
</html>";
    }
}
