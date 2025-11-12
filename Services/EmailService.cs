// --- SERVIÇO DE EMAIL ---
// Responsável por enviar emails de redefinição de senha

using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace APISinout.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string resetCode);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
        
        // Configurações SMTP do appsettings.json
        _smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        _smtpUsername = _configuration["Email:Username"] ?? "";
        _smtpPassword = _configuration["Email:Password"] ?? "";
        _fromEmail = _configuration["Email:FromEmail"] ?? "";
        _fromName = _configuration["Email:FromName"] ?? "Sinout - Sistema de Cuidados";
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetCode)
    {
        try
        {
            Console.WriteLine($"[EmailService] Preparando email para {toEmail}");
            Console.WriteLine($"[EmailService] Código de redefinição: {resetCode}");

            // Validar configurações
            if (string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword))
            {
                Console.WriteLine("[EmailService] ⚠️ MODO DEV: Credenciais de email não configuradas");
                Console.WriteLine($"[EmailService] 📧 Email que seria enviado para: {toEmail}");
                Console.WriteLine($"[EmailService] 🔑 Código de redefinição: {resetCode}");
                return;
            }

            var subject = "Redefinição de Senha - Sinout";
            var body = GenerateEmailBody(resetCode);

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

            await Task.Run(() => smtpClient.Send(mailMessage));
            Console.WriteLine($"[EmailService] ✅ Email enviado com sucesso para {toEmail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService] ❌ Erro ao enviar email: {ex.Message}");
            // Em desenvolvimento, apenas logar o erro sem falhar
            // Em produção, você pode querer lançar a exceção
            throw new Exception($"Erro ao enviar email: {ex.Message}");
        }
    }

    private string GenerateEmailBody(string resetCode)
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
}
