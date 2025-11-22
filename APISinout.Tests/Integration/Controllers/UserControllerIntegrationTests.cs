using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using APISinout.Models;
using APISinout.Tests.Fixtures;

namespace APISinout.Tests.Integration.Controllers;


public class UserControllerIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserControllerIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task SetupCuidadorAuth()
    {
        var cuidadorEmail = $"cuidador{Guid.NewGuid()}@test.com";
        var cuidadorPassword = "Cuidador@123";
        
        var registerRequest = new RegisterRequest
        {
            Name = "Cuidador User",
            Email = cuidadorEmail,
            Password = cuidadorPassword,
            Phone = "+55 11 99999-0002",
            PatientName = "Cuidador Patient"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = cuidadorEmail,
            Password = cuidadorPassword
        };

        await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
    }

    // Teste de admin removido

    [Fact]
    public async Task GetAll_AsCuidador_ShouldReturn403Forbidden()
    {
        // Arrange
        await SetupCuidadorAuth();


        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ShouldReturn401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ShouldReturn200WithUserData()
    {
        // Arrange
        await SetupCuidadorAuth();


        // Act
        var response = await _client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        user.Should().NotBeNull();
        user!.Email.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetCurrentUser_WithoutAuth_ShouldReturn401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Teste de admin removido

    [Fact]
    public async Task CreateUser_AsCuidador_ShouldReturn403Forbidden()
    {
        // Arrange
        await SetupCuidadorAuth();


        var request = new CreateUserRequest
        {
            Name = "New User",
            Email = $"newuser{Guid.NewGuid()}@test.com",
            Password = "NewUser@123",
            Role = "Cuidador"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdatePatientName_WithValidData_ShouldReturn200OK()
    {
        // Arrange
        await SetupCuidadorAuth();


        var request = new UpdatePatientNameRequest
        {
            PatientName = "Updated Patient Name"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/update-patient-name", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdatePatientName_WithoutAuth_ShouldReturn401Unauthorized()
    {
        // Arrange
        var request = new UpdatePatientNameRequest
        {
            PatientName = "Updated Patient Name"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/update-patient-name", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Teste de admin removido

    [Fact]
    public async Task GetCuidadores_AsCuidador_ShouldReturn403Forbidden()
    {
        // Arrange
        await SetupCuidadorAuth();


        // Act
        var response = await _client.GetAsync("/api/users/cuidadores");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // Teste de admin removido

    // Teste de admin removido
}
