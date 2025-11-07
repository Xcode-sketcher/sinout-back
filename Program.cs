// --- 1. A LISTA DE COMPRAS (IMPORTS / USINGS) ---
// Estes são TODOS os 'usings' que você precisa para a Semana 1 e 2.
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using BCrypt.Net;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Microsoft.AspNetCore.Authentication.JwtBearer; // O "Segurança"
using Microsoft.IdentityModel.Tokens;               // Ferramenta do Token
using System.Text;                                  // Ferramenta de codificação
using System.IdentityModel.Tokens.Jwt;              // O "Crachá" (Token)
using System.Security.Claims;                       // As "Informações" no crachá
using Microsoft.AspNetCore.Authorization;           // Para trancar rotas

// --- 2. A CHAVE SECRETA (A GRANDE CORREÇÃO) ---
// Definimos a chave secreta AQUI, no topo, para que
// tanto o "Setup" quanto a "Rota de Login" possam vê-la.
const string JwtSecretKey = "sua-chave-secreta-muito-longa-e-dificil-de-adivinhar-123";

// --- 3. ABRINDO A COZINHA (SETUP) ---
var builder = WebApplication.CreateBuilder(args);

// --- 3a. Configurando o "Mongoloide" (MongoDB) ---
var mongoSettings = builder.Configuration.GetSection("MongoDbSettings");
var mongoClient = new MongoClient(mongoSettings["ConnectionString"]);
var mongoDatabase = mongoClient.GetDatabase(mongoSettings["DatabaseName"]);
builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);

// --- 3b. Habilitando o Swagger (Postman Embutido) ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Esta parte é um BÔNUS: Adiciona um botão "Authorize" no Swagger
    // para que você possa "usar" o crachá (token) nos testes.
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Insira 'Bearer' [espaço] e depois o seu token."
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// --- 3c. Configurando o JWT (O Gerente de Segurança) ---
// "Ensina" o .NET a como ler e validar os crachás (tokens).
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        // Diz ao Segurança para usar a MESMA chave secreta que definimos lá em cima.
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecretKey)),
        ValidateIssuer = false, // Não valida quem emitiu
        ValidateAudience = false // Não valida para quem foi emitido
    };
});

// Adiciona o serviço de "Autorização" (o que cada perfil pode fazer)
builder.Services.AddAuthorization();


// --- 4. CONSTRUINDO A API (Ligando o Chefe) ---
var app = builder.Build();

// Configura o Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// (Obrigatório) Redireciona HTTP para HTTPS
app.UseHttpsRedirection();

// --- 4b. CONTRATANDO O SEGURANÇA (Ordem Importa!) ---
// "Contrata" o Segurança na porta da cozinha.
// DEVE vir DEPOIS do UseHttpsRedirection, mas ANTES das rotas (Map...).
app.UseAuthentication(); // "Segurança, verifique os crachás."
app.UseAuthorization();  // "Segurança, verifique se o perfil pode entrar."


// --- 5. AS COMANDAS (ROTAS / ENDPOINTS) ---

// --- 5a. Rota de Cadastro (Semana 1) ---
app.MapPost("/cuidadores", async (
    [FromBody] Cuidador novoCuidador,
    [FromServices] IMongoDatabase database
) =>
{
    var collection = database.GetCollection<Cuidador>("cuidadores");

    if (string.IsNullOrEmpty(novoCuidador.Email) || string.IsNullOrEmpty(novoCuidador.Senha))
    {
        return Results.BadRequest(new { erro = "Email e senha são obrigatórios" });
    }

    var emailJaExiste = await collection.Find(c => c.Email == novoCuidador.Email).AnyAsync();
    if (emailJaExiste)
    {
        return Results.Conflict(new { erro = "Email já cadastrado" });
    }

    var senhaHash = BCrypt.Net.BCrypt.HashPassword(novoCuidador.Senha);
    novoCuidador.Senha = senhaHash;
    novoCuidador.Perfil = "cuidador"; // Perfil padrão

    await collection.InsertOneAsync(novoCuidador);

    // Limpa a senha antes de retornar, por segurança.
    novoCuidador.Senha = "---";
    return Results.Created($"/cuidadores/{novoCuidador.Id}", novoCuidador);
});

// --- 5b. Rota de Login (Semana 2 - CORRIGIDA) ---
app.MapPost("/login", async (
    [FromBody] CuidadorLoginRequest loginRequest, // <-- Usa a nova classe
    [FromServices] IMongoDatabase database
) =>
{
    var collection = database.GetCollection<Cuidador>("cuidadores");

    // Lógica 1: Busca o usuário
    var usuarioNoBanco = await collection.Find(c => c.Email == loginRequest.Email).FirstOrDefaultAsync();

    // Lógica 2: Verifica a senha
    if (usuarioNoBanco == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Senha, usuarioNoBanco.Senha))
    {
        // Linha correta
        return Results.Json(new { erro = "Email ou senha inválidos" }, statusCode: 401);
    }

    // Lógica 3: Criar o Crachá (Token)
    // Pega a chave secreta lá do topo do arquivo
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecretKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    // Informações que vão DENTRO do crachá
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, usuarioNoBanco.Id!), // O ID do usuário
        new Claim(ClaimTypes.Role, usuarioNoBanco.Perfil)         // O Perfil (cuidador, admin, etc)
    };

    // Cria o crachá
    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.Now.AddHours(8), // Duração de 8 horas
        signingCredentials: creds
    );

    // Converte para string
    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    // Retorna o crachá para o usuário
    return Results.Ok(new { token = tokenString });
});

// --- 5c. Rota Protegida (Semana 2 - CORRIGIDA) ---
// Esta rota agora está "trancada"
app.MapGet("/meu-perfil", async (
    [FromServices] IMongoDatabase database,
    HttpContext http // 'HttpContext' nos deixa ler o crachá do usuário logado
) =>
{
    // "Do crachá (http.User), pegue a informação (Claim) do tipo 'ID' (NameIdentifier)"
    var idDoUsuarioNoCracha = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (idDoUsuarioNoCracha == null)
    {
        // Isso não deveria acontecer se o [Authorize] funcionou, mas é uma boa checagem.
        // Linha correta
        return Results.Json(new { erro = "Token inválido" }, statusCode: 401);
    }

    // Busca o usuário no banco usando o ID que lemos do crachá
    var collection = database.GetCollection<Cuidador>("cuidadores");
    var usuario = await collection.Find(c => c.Id == idDoUsuarioNoCracha).FirstOrDefaultAsync();

    if (usuario == null)
    {
        return Results.NotFound(new { erro = "Usuário do token não encontrado" });
    }

    usuario.Senha = "--- CONFIDENCIAL ---"; // NUNCA retorne a senha
    return Results.Ok(usuario);

}).RequireAuthorization(); // <-- ISSO AQUI "TRANCA" A ROTA.


// --- 6. LIGANDO A COZINHA (RUN) ---
app.Run();

// --- 7. AS "PLANTAS" (CLASSES / MODELOS) ---
// (Elas DEVEM vir DEPOIS do app.Run())

// A "Forma" do Cuidador (que você já tinha)
public class Cuidador
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("senha")]
    public string Senha { get; set; } = string.Empty;

    [BsonElement("nome")]
    public string? Nome { get; set; }

    [BsonElement("perfil")]
    public string Perfil { get; set; } = string.Empty;

    // --- BÔNUS: PREPARAÇÃO PARA SEMANA 3 ---
    // Já vamos adicionar o campo para os dados faciais,
    // assim o modelo já está pronto.
    [BsonElement("dadosFaciais")]
    [BsonIgnoreIfNull] // Se for nulo, não salva no Mongo (economiza espaço)
    public object? DadosFaciais { get; set; } // 'object' significa "qualquer tipo de JSON"
}

// A "Forma" do JSON que o /login espera (A GRANDE CORREÇÃO)
public class CuidadorLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}