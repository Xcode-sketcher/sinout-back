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
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Test User Integration",
            Email = $"integration{Guid.NewGuid()}@test.com",
            Password = "Test@123",
            Phone = "+55 11 99999-9999",
            PatientName = "Patient Test",
            Role = "Cuidador"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Check if HttpOnly cookies were set
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        cookies.Should().Contain(cookie => cookie.Contains("accessToken=") && cookie.Contains("httponly"));

        // Response body should contain user info
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("\"user\"");
        responseContent.Should().Contain("\"email\"");
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

        // Check if HttpOnly cookies were set
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        cookies.Should().Contain(cookie => cookie.Contains("accessToken=") && cookie.Contains("httponly"));

        // Response body should contain user info
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("\"user\"");
        responseContent.Should().Contain("\"email\"");
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

        // Check register cookies
        var registerCookies = registerResponse.Headers.GetValues("Set-Cookie").ToList();
        registerCookies.Should().Contain(cookie => cookie.Contains("accessToken=") && cookie.Contains("httponly"));

        // Register response should contain user info
        var registerContent = await registerResponse.Content.ReadAsStringAsync();
        registerContent.Should().Contain("\"user\"");

        // Step 2: Login
        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Check login cookies
        var loginCookies = loginResponse.Headers.GetValues("Set-Cookie").ToList();
        loginCookies.Should().Contain(cookie => cookie.Contains("accessToken=") && cookie.Contains("httponly"));

        // Login response should contain user info
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        loginContent.Should().Contain("\"user\"");
    }
}
