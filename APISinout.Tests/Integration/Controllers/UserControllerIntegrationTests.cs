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

    // GetAll_AsCuidador_ShouldReturn403Forbidden removed as route /api/users no longer exists

    // GetAll_WithoutAuth_ShouldReturn401Unauthorized removed as route /api/users no longer exists

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

    // CreateUser_AsCuidador_ShouldReturn403Forbidden removed as route /api/users no longer exists

    // UpdatePatientName endpoint was removed - patient management now handled through Patient repository

    // Teste de admin removido

    // GetCuidadores_AsCuidador_ShouldReturn403Forbidden removed as route /api/users/cuidadores no longer exists

    // Teste de admin removido

    // Teste de admin removido
}
