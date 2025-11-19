// --- RECEITA DA API SINOUT: A COZINHA DIGITAL ---
// Imagine que nossa API é como uma cozinha de restaurante!
// Aqui no Program.cs, estamos preparando todos os "ingredientes" e "utensílios" necessários
// antes de abrir as portas para os clientes (usuários).
// Vamos organizar tudo como um chef organizando sua cozinha antes do expediente!

// --- 1. A LISTA DE COMPRAS (IMPORTS / USINGS) ---
// Estes são os "ingredientes especiais" que vamos usar na receita.
// Como um chef que compra temperos de diferentes lojas, aqui importamos bibliotecas de vários lugares.
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using BCrypt.Net;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Microsoft.AspNetCore.Authentication.JwtBearer; // O "Segurança" - como um cadeado na porta da cozinha
using Microsoft.IdentityModel.Tokens;               // Ferramenta do Token - a chave do cadeado
using System.Text;                                  // Ferramenta de codificação - misturar os sabores
using System.IdentityModel.Tokens.Jwt;              // O "Crachá" (Token) - o crachá do funcionário
using System.Security.Claims;                       // As "Informações" no crachá - nome e cargo
using Microsoft.AspNetCore.Authorization;           // Para trancar rotas - portas trancadas da cozinha
using APISinout.Data;
using APISinout.Services;
using APISinout.Validators;
using FluentValidation.AspNetCore;
using FluentValidation;
using Scalar.AspNetCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

// --- 2. A CHAVE SECRETA (A GRANDE CORREÇÃO) ---
// Esta é a "receita secreta" da família! Guardamos ela aqui no topo,
// pois tanto o "Setup" quanto a "Rota de Login" precisam dela.
// É como a fórmula secreta do molho especial que só o chef sabe.
var builder = WebApplication.CreateBuilder(args);

// --- 3. ORGANIZANDO A COZINHA (SERVIÇOS) ---
// Agora vamos "montar" nossa cozinha: adicionar os utensílios e ingredientes básicos.
// Como um chef organizando panelas, facas e ingredientes na bancada.

builder.Services.AddControllers();

// Configuração do MongoDB - O "freezer" onde guardamos os ingredientes frescos
builder.Services.AddSingleton<MongoDbContext>();

// Repositórios - Acesso aos dados
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IEmotionMappingRepository, EmotionMappingRepository>();
builder.Services.AddScoped<IHistoryRepository, HistoryRepository>();
builder.Services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();

// Serviços de Negócio - Lógica da aplicação
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IEmotionMappingService, EmotionMappingService>();
builder.Services.AddScoped<IHistoryService, HistoryService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Serviços de Infraestrutura
builder.Services.AddSingleton<IRateLimitService, RateLimitService>();
builder.Services.AddHostedService<TokenCleanupService>();

// Validação - O "inspetor de qualidade" que checa se os ingredientes estão bons
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

// Autenticação JWT - O sistema de crachás para entrar na cozinha
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// Autorização - As "regras da casa" sobre quem pode entrar onde
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("Admin"));
});

// CORS - Permitir chamadas do frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Documentação da API - O "cardápio" que mostramos aos clientes
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API Sinout", Version = "v1" });
    
    // Configuração para JWT - Como explicar no cardápio que precisa de crachá
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Limite de taxa de pedidos (Rate Limiting)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // 1. Limite para Autenticação (Login/Register) - Por IP
    options.AddPolicy("limite-auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromSeconds(10)
            }));

    // 2. Limite Geral da API - Por IP (ou Usuário se autenticado)
    options.AddPolicy("limite-api", context =>
    {
        // Se usuário estiver autenticado, usa o ID, senão usa o IP
        var partitionKey = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey!,
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 60, // 60 requisições por minuto
                QueueLimit = 2,
                Window = TimeSpan.FromMinutes(1)
            });
    });

    // 3. Limite para Detecção de Emoções (CuidadorEmotion) - Por Usuário
    options.AddPolicy("limite-emotion", context =>
    {
        var partitionKey = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey!,
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 30, // 30 requisições por minuto (1 a cada 2s em média)
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            });
    });
});

// --- 4. ABRINDO A COZINHA (CONFIGURANDO O APP) ---
var app = builder.Build();

// --- 5. PREPARANDO O AMBIENTE DE TRABALHO ---
// Como um chef verificando se tudo está limpo e organizado antes de abrir
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // O "primeiro socorros" para problemas durante o desenvolvimento
    app.UseSwagger(); // Publicar o cardápio
    app.UseSwaggerUI(); // Mostrar o cardápio em uma página bonita
    app.MapScalarApiReference(options => options.WithOpenApiRoutePattern("/swagger/v1/swagger.json")); // Cardápio interativo
}

app.UseHttpsRedirection(); // Como redirecionar clientes para a entrada segura

app.UseCors("AllowAll"); // Permitir CORS

app.UseAuthentication(); // Verificar os crachás na porta
app.UseAuthorization(); // Decidir quem entra onde
app.UseCors("AllowAll"); // Usar a política de CORS definida
app.UseRateLimiter();
app.MapControllers(); // Abrir as portas da cozinha para os pedidos!


app.Run(); // Ligar as luzes e abrir as portas - a cozinha está funcionando!

// Tornar a classe Program acessível para testes de integração
public partial class Program { }