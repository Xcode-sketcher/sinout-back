using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using APISinout.Models;
using APISinout.Tests.Fixtures;

namespace APISinout.Tests.Integration.Controllers;

/// <summary>
/// Testes de integração para AuthController
/// Testa fluxos completos de autenticação
/// </summary>
public class AuthControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturn201Created()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Test User Integration",
            Email = $"integration{Guid.NewGuid()}@test.com",
            Password = "Test@123",
            Phone = "+55 11 99999-9999",
            PatientName = "Patient Test",
            Role = "Caregiver"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.User.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturn400BadRequest()
    {
        // Arrange
        var email = $"duplicate{Guid.NewGuid()}@test.com";
        var request1 = new RegisterRequest
        {
            Name = "First User",
            Email = email,
            Password = "Test@123",
            Phone = "+55 11 99999-9999",
            PatientName = "Patient Test"
        };

        // First registration
        await _client.PostAsJsonAsync("/api/auth/register", request1);

        // Try to register again with same email
        var request2 = new RegisterRequest
        {
            Name = "Second User",
            Email = email,
            Password = "Test@456",
            Phone = "+55 11 88888-8888",
            PatientName = "Another Patient"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Weak Pass User",
            Email = $"weakpass{Guid.NewGuid()}@test.com",
            Password = "Test@1", // too short/weak as per validator
            Phone = "+55 11 99999-9999",
            PatientName = "Patient Test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithEmptyPassword_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Empty Pass User",
            Email = $"emptypass{Guid.NewGuid()}@test.com",
            Password = "", // empty password
            Phone = "+55 11 99999-9999",
            PatientName = "Patient Test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturn200OK()
    {
        // Arrange - First register a user
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

        // Now login
        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturn401Unauthorized()
    {
        // Arrange - First register a user
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

        // Try login with wrong password
        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = "WrongPassword123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FullAuthFlow_RegisterAndLogin_ShouldWork()
    {
        // Arrange
        var email = $"fullflow{Guid.NewGuid()}@test.com";
        var password = "Test@123";

        // Step 1: Register
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

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var registrationToken = registerResult!.Token;

        // Step 2: Login
        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var loginToken = loginResult!.Token;

        // Assert
        registrationToken.Should().NotBeNullOrEmpty();
        loginToken.Should().NotBeNullOrEmpty();
        loginResult.User.Email.Should().Be(email.ToLower());
    }
}
