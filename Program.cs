// --- 1. A LISTA DE COMPRAS (IMPORTS / USINGS) ---
// Estes s�o TODOS os 'usings' que voc� precisa para a Semana 1 e 2.
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using BCrypt.Net;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Microsoft.AspNetCore.Authentication.JwtBearer; // O "Seguran�a"
using Microsoft.IdentityModel.Tokens;               // Ferramenta do Token
using System.Text;                                  // Ferramenta de codifica��o
using System.IdentityModel.Tokens.Jwt;              // O "Crach�" (Token)
using System.Security.Claims;                       // As "Informa��es" no crach�
using Microsoft.AspNetCore.Authorization;           // Para trancar rotas
using APISinout.Data;
using APISinout.Services;
using APISinout.Validators;
using FluentValidation.AspNetCore;
using FluentValidation;
using Scalar.AspNetCore;
using Microsoft.OpenApi.Models;

// --- 2. A CHAVE SECRETA (A GRANDE CORRE��O) ---
// Definimos a chave secreta AQUI, no topo, para que
// tanto o "Setup" quanto a "Rota de Login" possam v�-la.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Configuração do MongoDB
builder.Services.AddSingleton<MongoDbContext>();

// Serviços de Autenticação
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Validação
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

// Autenticação JWT
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

// Autorização
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("Admin"));
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API Sinout", Version = "v1" });
    
    // Configuração para JWT
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
    // app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapScalarApiReference(options => options.WithOpenApiRoutePattern("/swagger/v1/swagger.json"));

app.Run();