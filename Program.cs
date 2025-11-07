// --- 1. A LISTA DE COMPRAS (IMPORTS) ---
// Em C#, chamamos de "usings". São as bibliotecas que vamos usar.
using Microsoft.AspNetCore.Mvc; // Traz ferramentas de API (como [FromBody])
using MongoDB.Driver;          // O driver do MongoDB (o nosso "pymongo")
using BCrypt.Net;             // O "cofre" de senhas (o nosso "bcrypt")
using System.Security.Claims; // (Não vamos usar ainda, mas é para o JWT)
using MongoDB.Bson;           // Ferramenta para lidar com IDs do Mongo (o "ObjectId")
using MongoDB.Bson.Serialization.Attributes; // Ferramenta para mapear IDs

// --- 2. O "MODELO" (O FORMATO DO NOSSO USUÁRIO) ---
// Em C#, não usamos dicionários soltos. Criamos uma "Forma" (Classe)
// para dizer como nossos dados devem se parecer.
// Esta é a "planta" do nosso documento 'cuidador' no Mongo.


// --- 3. ABRINDO A COZINHA (SETUP) ---
// Em .NET, tudo começa com um "construtor" (builder).
var builder = WebApplication.CreateBuilder(args);

// --- 3a. Configurando a Conexão com o "Mongoloide" (MongoDB) ---
// Esta é a parte "chique" do .NET. Chama-se "Injeção de Dependência".
// Basicamente: "Ensine" o .NET a como se conectar no Mongo UMA VEZ.
// 1. Leia as configurações que colocamos no 'appsettings.json'
var mongoSettings = builder.Configuration.GetSection("MongoDbSettings");
// 2. Crie um "cliente" (a ligação) com a ConnectionString
var mongoClient = new MongoClient(mongoSettings["ConnectionString"]);
// 3. Pegue o "banco de dados" (o armário)
var mongoDatabase = mongoClient.GetDatabase(mongoSettings["DatabaseName"]);

// 4. "Registre" o banco de dados na aplicação.
// Agora, qualquer rota que "pedir" pelo banco (IMongoDatabase), o .NET vai entregar.
builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);

// --- 3b. Habilitando o "Postman Embutido" (Swagger) ---
// Isso cria a página de documentação interativa para testarmos.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- 4. CONSTRUINDO A API (Ligando o Chefe) ---
// "Construtor, agora crie a Cozinha (o app)."
var app = builder.Build();

// Configura o Swagger (só funciona se estivermos em modo "Desenvolvimento")
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// (Obrigatório) Redireciona HTTP para HTTPS
app.UseHttpsRedirection();

// --- 5. AS COMANDAS (ROTAS / ENDPOINTS) ---
// Esta é a parte que você conhece! É o nosso "Plano da Semana 1".
// Em Flask: @app.route("/cuidadores", methods=["POST"])
// Em .NET:  app.MapPost("/cuidadores", ...)

app.MapPost("/cuidadores", async (
    [FromBody] Cuidador novoCuidador, // [FromBody] diz: "Pegue o JSON do body e tente encaixar na 'forma' Cuidador"
    [FromServices] IMongoDatabase database // [FromServices] diz: "Me dê aquela conexão do Mongo que você registrou"
) =>
{
    // "async" e "await" são o jeito do C# de lidar com coisas demoradas (como I/O de rede/banco)
    // sem travar a aplicação. Pense neles como o ".then()" do JavaScript.

    // Pega a "gaveta" (coleção) de cuidadores.
    var collectionName = app.Configuration.GetSection("MongoDbSettings")["CollectionName"];
    var collection = database.GetCollection<Cuidador>(collectionName);

    // --- Verificação 1: Email e senha vieram? ---
    // (O C# é mais verboso que o Python)
    if (string.IsNullOrEmpty(novoCuidador.Email) || string.IsNullOrEmpty(novoCuidador.Senha))
    {
        // Em .NET, usamos Results.BadRequest() (Erro 400)
        return Results.BadRequest(new { erro = "Email e senha são obrigatórios" });
    }

    // --- Verificação 2: Email já existe? ---
    // "Vá na gaveta e veja se encontra (Find) ALGUM (Any) documento onde o email é igual."
    var emailJaExiste = await collection.Find(c => c.Email == novoCuidador.Email).AnyAsync();

    if (emailJaExiste)
    {
        // Em .NET, usamos Results.Conflict() (Erro 409)
        return Results.Conflict(new { erro = "Email já cadastrado" });
    }

    // --- "Embaralhando" a senha com o Bcrypt ---
    // Exatamente como no Python, mas com sintaxe C#.
    // Usamos o 'BCrypt.Net.BCrypt' (a biblioteca que instalamos).
    var senhaHash = BCrypt.Net.BCrypt.HashPassword(novoCuidador.Senha);

    // --- Preparando o Documento ---
    // O objeto 'novoCuidador' que recebemos já é quase o que queremos.
    // Só precisamos trocar a senha pura pelo HASH e definir o perfil.
    novoCuidador.Senha = senhaHash;
    novoCuidador.Perfil = "cuidador"; // Define o perfil padrão

    // --- Salvando no Banco ---
    // "Insira UM (InsertOne) documento na gaveta."
    // O "Async" no final do nome do método é padrão em C#.
    await collection.InsertOneAsync(novoCuidador);

    // Retorna Sucesso (201 - Criado)
    // Note que 'novoCuidador.Id' agora foi preenchido pelo Mongo!
    return Results.Created($"/cuidadores/{novoCuidador.Id}", new { mensagem = "Cuidador criado!", id = novoCuidador.Id });
});

// --- 6. LIGANDO A COZINHA (RUN) ---
// "App, comece a escutar por pedidos."
app.Run();
public class Cuidador
{
    // O ID do Mongo. As anotações [BsonId] e [BsonRepresentation]
    // fazem o C# entender e converter o "_id" esquisito do Mongo.
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; } // O '?' diz que pode ser nulo (ao criar um novo)

    [BsonElement("email")] // O nome do campo no Mongo
    public string Email { get; set; } = string.Empty;

    [BsonElement("senha")] // O nome do campo no Mongo
    public string Senha { get; set; } = string.Empty;

    [BsonElement("nome")] // O nome do campo no Mongo
    public string? Nome { get; set; } // Pode ser nulo

    [BsonElement("perfil")] // O nome do campo no Mongo
    public string Perfil { get; set; } = string.Empty;
}