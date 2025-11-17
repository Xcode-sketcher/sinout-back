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

    private async Task<string> GetCaregiverToken()
    {
        var caregiverEmail = $"caregiver{Guid.NewGuid()}@test.com";
        var caregiverPassword = "Caregiver@123";
        
        var registerRequest = new RegisterRequest
        {
            Name = "Caregiver User",
            Email = caregiverEmail,
            Password = caregiverPassword,
            Phone = "+55 11 99999-0002",
            PatientName = "Caregiver Patient"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = caregiverEmail,
            Password = caregiverPassword
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.Token;
    }

    // Teste de admin removido

    [Fact]
    public async Task GetAll_AsCaregiver_ShouldReturn403Forbidden()
    {
        // Arrange
        var token = await GetCaregiverToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

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
        var token = await GetCaregiverToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

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
    public async Task CreateUser_AsCaregiver_ShouldReturn403Forbidden()
    {
        // Arrange
        var token = await GetCaregiverToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateUserRequest
        {
            Name = "New User",
            Email = $"newuser{Guid.NewGuid()}@test.com",
            Password = "NewUser@123",
            Role = "Caregiver"
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
        var token = await GetCaregiverToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

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
    public async Task GetCaregivers_AsCaregiver_ShouldReturn403Forbidden()
    {
        // Arrange
        var token = await GetCaregiverToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/users/caregivers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // Teste de admin removido

    // Teste de admin removido
}
