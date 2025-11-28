using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using APISinout.Models;
using APISinout.Tests.Fixtures;

namespace APISinout.Tests.Integration.Controllers;


public class AuthControllerIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturn201Created()
    {
        // Arrange - Prepara dados de registro
        var request = new RegisterRequest
        {
            Name = "Test User Integration",
            Email = $"integration{Guid.NewGuid()}@test.com",
            Password = "Test@123",
            Phone = "+55 11 99999-9999",
            PatientName = "Patient Test",
            Role = "Cuidador"
        };

        // Act - Envia requisição de registro
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert - Verifica se retornou Created e conteúdo da resposta
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Assert - Verificar se cookies HttpOnly foram definidos
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        cookies.Should().Contain(cookie => cookie.Contains("accessToken=") && cookie.Contains("httponly"));

        // O corpo da resposta deve conter informações do usuário
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("\"user\"");
        responseContent.Should().Contain("\"email\"");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturn400BadRequest()
    {
        // Arrange - Prepara dados e mocks para o teste
        var email = $"duplicate{Guid.NewGuid()}@test.com";
        var request1 = new RegisterRequest
        {
            Name = "First User",
            Email = email,
            Password = "Test@123",
            Phone = "+55 11 99999-9999",
            PatientName = "Patient Test"
        };

        // Primeiro registro
        await _client.PostAsJsonAsync("/api/auth/register", request1);

        // Tentar registrar novamente com o mesmo email
        var request2 = new RegisterRequest
        {
            Name = "Second User",
            Email = email,
            Password = "Test@456",
            Phone = "+55 11 88888-8888",
            PatientName = "Another Patient"
        };

        // Act - Executa a ação a ser testada
        var response = await _client.PostAsJsonAsync("/api/auth/register", request2);

        // Assert - Verifica o resultado esperado
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ShouldReturn400BadRequest()
    {
        // Arrange - Prepara dados para o teste
        var request = new RegisterRequest
        {
            Name = "Weak Pass User",
            Email = $"weakpass{Guid.NewGuid()}@test.com",
            Password = "Test@1", // muito curta/fraca conforme o validador
            Phone = "+55 11 99999-9999",
            PatientName = "Patient Test"
        };

        // Act - Executa a requisição
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert - Verifica o resultado
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithEmptyPassword_ShouldReturn400BadRequest()
    {
        // Arrange - Prepara dados para o teste (senha vazia)
        var request = new RegisterRequest
        {
            Name = "Empty Pass User",
            Email = $"emptypass{Guid.NewGuid()}@test.com",
            Password = "", // senha vazia
            Phone = "+55 11 99999-9999",
            PatientName = "Patient Test"
        };

        // Act - Envia requisição de login
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert - Verifica status OK e cookies HttpOnly
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturn200OK()
    {
        // Arrange - Registrar um usuário inicialmente
        var email = $"login{Guid.NewGuid()}@test.com";
        var password = "Test@123";
        var registerRequest = new RegisterRequest
        {
            Name = "Login Test User",
            Email = email,
            Password = password,
            Phone = "+55 11 99999-9999",
            PatientName = "Patient Test"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Efetuar login
        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        // Act - Envia requisição de login com senha incorreta
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert - Verifica que retorna Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert - Verificar se os cookies HttpOnly foram definidos
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        cookies.Should().Contain(cookie => cookie.Contains("accessToken=") && cookie.Contains("httponly"));

        // O corpo da resposta deve conter informações do usuário
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("\"user\"");
        responseContent.Should().Contain("\"email\"");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturn401Unauthorized()
    {
        // Arrange - Registrar um usuário inicialmente
        var email = $"wrongpass{Guid.NewGuid()}@test.com";
        var registerRequest = new RegisterRequest
        {
            Name = "Wrong Pass Test",
            Email = email,
            Password = "Test@123",
            Phone = "+55 11 99999-9999",
            PatientName = "Patient Test"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Tentar logar com senha incorreta
        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = "WrongPassword123"
        };

        // Act - Conduz registro e login no fluxo completo
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert - Verifica cookies e conteúdo das respostas para registro e login
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FullAuthFlow_RegisterAndLogin_ShouldWork()
    {
        // Arrange - Registrar um usuário inicialmente para login
        var email = $"fullflow{Guid.NewGuid()}@test.com";
        var password = "Test@123";

        // Etapa 1: Registro
        var registerRequest = new RegisterRequest
        {
            Name = "Full Flow User",
            Email = email,
            Password = password,
            Phone = "+55 11 99999-9999",
            PatientName = "Full Flow Patient"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Assert - Verificar cookies de registro
        var registerCookies = registerResponse.Headers.GetValues("Set-Cookie").ToList();
        registerCookies.Should().Contain(cookie => cookie.Contains("accessToken=") && cookie.Contains("httponly"));

        // A resposta do registro deve conter informações do usuário
        var registerContent = await registerResponse.Content.ReadAsStringAsync();
        registerContent.Should().Contain("\"user\"");

        // Etapa 2: Login
        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert - Verificar cookies de login
        var loginCookies = loginResponse.Headers.GetValues("Set-Cookie").ToList();
        loginCookies.Should().Contain(cookie => cookie.Contains("accessToken=") && cookie.Contains("httponly"));

        // A resposta do login deve conter informações do usuário
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        loginContent.Should().Contain("\"user\"");
    }

    [Fact]
    public async Task Logout_AfterLogin_ShouldClearCookieAndReturn401OnProtectedRoute()
    {
        // Arrange - Registrar e autenticar um usuário
        var email = $"logout{Guid.NewGuid()}@test.com";
        var password = "Test@123";
        var registerRequest = new RegisterRequest
        {
            Name = "Logout Test User",
            Email = email,
            Password = password,
            Phone = "+55 11 99999-9999",
            PatientName = "Logout Patient"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Act - Efetuar logout
        var logoutResponse = await _client.PostAsync("/api/auth/logout", null);

        // Assert - Logout deve ter sucesso
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert - Verificar se o cookie foi limpo (deve ter data de expiração)
        var logoutCookies = logoutResponse.Headers.GetValues("Set-Cookie").ToList();
        logoutCookies.Should().Contain(cookie => cookie.Contains("accessToken=") && cookie.Contains("expires="));

        // O corpo da resposta deve conter mensagem de sucesso
        var logoutContent = await logoutResponse.Content.ReadAsStringAsync();
        logoutContent.Should().Contain("Logout realizado com sucesso");

        // Agora tente acessar uma rota protegida - deve retornar 401
        var protectedResponse = await _client.GetAsync("/api/auth/me");
        protectedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
