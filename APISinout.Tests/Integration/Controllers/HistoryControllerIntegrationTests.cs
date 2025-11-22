using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using APISinout.Models;
using APISinout.Tests.Fixtures;

namespace APISinout.Tests.Integration.Controllers;


public class HistoryControllerIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public HistoryControllerIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<int> GetCuidadorUserId(HttpClient? client = null)
    {
        var httpClient = client ?? _client;
        var cuidadorEmail = $"cuidador{Guid.NewGuid()}@test.com";
        var cuidadorPassword = "Cuidador@123";
        
        var registerRequest = new RegisterRequest
        {
            Name = "Cuidador User",
            Email = cuidadorEmail,
            Password = cuidadorPassword,
            Phone = "+55 11 99999-2002",
            PatientName = "Cuidador Patient"
        };

        await httpClient.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = cuidadorEmail,
            Password = cuidadorPassword
        };

        await httpClient.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Get current user info using cookies
        var userResponse = await httpClient.GetFromJsonAsync<UserResponse>("/api/users/me");
        userResponse.Should().NotBeNull();
        return userResponse!.UserId;
    }

    [Fact]
    public async Task GetMyHistory_WithValidToken_ShouldReturn200OK()
    {
        // Arrange
        var userId = await GetCuidadorUserId();

        // Seed data
        var request = new
        {
            cuidadorId = userId,
            patientName = "Test Patient",
            dominantEmotion = "happy",
            emotionsDetected = new Dictionary<string, double> { { "happy", 0.9 } },
            timestamp = DateTime.UtcNow
        };
        await _client.PostAsJsonAsync("/api/history/cuidador-emotion", request);

        // Act
        var response = await _client.GetAsync("/api/history/my-history?hours=24");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await response.Content.ReadFromJsonAsync<List<HistoryRecord>>();
        history.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMyHistory_WithoutAuth_ShouldReturn401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/history/my-history?hours=24");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyHistory_WithCustomHours_ShouldReturn200OK()
    {
        // Arrange
        var userId = await GetCuidadorUserId();


        // Seed data
        var request = new
        {
            cuidadorId = userId,
            patientName = "Test Patient",
            dominantEmotion = "happy",
            emotionsDetected = new Dictionary<string, double> { { "happy", 0.9 } },
            timestamp = DateTime.UtcNow
        };
        await _client.PostAsJsonAsync("/api/history/cuidador-emotion", request);

        // Act
        var response = await _client.GetAsync("/api/history/my-history?hours=48");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await response.Content.ReadFromJsonAsync<List<HistoryRecord>>();
        history.Should().NotBeNull();
    }
    [Fact]
    public async Task GetMyHistory_WithCustomHours_ShouldReturn400BadRequest()
    {
        // Arrange
        var userId = await GetCuidadorUserId();


        // Seed data to avoid NotFound
        var request = new
        {
            cuidadorId = userId,
            patientName = "Test Patient",
            dominantEmotion = "happy",
            emotionsDetected = new Dictionary<string, double> { { "happy", 0.9 } },
            timestamp = DateTime.UtcNow
        };
        await _client.PostAsJsonAsync("/api/history/cuidador-emotion", request);

        // Act
        var response = await _client.GetAsync("/api/history/my-history?hours=12");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMyHistory_WithCustomHours_ShouldReturn404NotFound()
    {
        // Arrange
        await GetCuidadorUserId();


        // Act
        var response = await _client.GetAsync("/api/history/my-history?hours=24");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHistoryByUser_AsOwner_ShouldReturn200OK()
    {
        // Arrange
        var userId = await GetCuidadorUserId();


        // Seed data
        var request = new
        {
            cuidadorId = userId,
            patientName = "Test Patient",
            dominantEmotion = "happy",
            emotionsDetected = new Dictionary<string, double> { { "happy", 0.9 } },
            timestamp = DateTime.UtcNow
        };
        await _client.PostAsJsonAsync("/api/history/cuidador-emotion", request);

        // Act
        var response = await _client.GetAsync($"/api/history/user/{userId}?hours=24");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await response.Content.ReadFromJsonAsync<List<HistoryRecord>>();
        history.Should().NotBeNull();
    }

    // Teste de admin removido

    [Fact]
    public async Task GetHistoryByUser_AsNonOwner_ShouldReturn400BadRequest()
    {
        // Arrange
        var userId1 = await GetCuidadorUserId();
        var client2 = _factory.CreateClientWithCookies();
        var userId2 = await GetCuidadorUserId(client2);

        // Act - Try to access another user's history
        var response = await _client.GetAsync($"/api/history/user/{userId2}?hours=24");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMyStatistics_WithValidToken_ShouldReturn200OK()
    {
        // Arrange
        var userId = await GetCuidadorUserId();


        // Seed data
        var request = new
        {
            cuidadorId = userId,
            patientName = "Test Patient",
            dominantEmotion = "happy",
            emotionsDetected = new Dictionary<string, double> { { "happy", 0.9 } },
            timestamp = DateTime.UtcNow
        };
        await _client.PostAsJsonAsync("/api/history/cuidador-emotion", request);

        // Act
        var response = await _client.GetAsync("/api/history/statistics/my-stats?hours=24");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<object>();
        stats.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMyStatistics_WithoutAuth_ShouldReturn401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/history/statistics/my-stats?hours=24");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyStatistics_WithCustomHours_ShouldReturn200OK()
    {
        // Arrange
        var userId = await GetCuidadorUserId();


        // Seed data
        var request = new
        {
            cuidadorId = userId,
            patientName = "Test Patient",
            dominantEmotion = "happy",
            emotionsDetected = new Dictionary<string, double> { { "happy", 0.9 } },
            timestamp = DateTime.UtcNow
        };
        await _client.PostAsJsonAsync("/api/history/cuidador-emotion", request);

        // Act
        var response = await _client.GetAsync("/api/history/statistics/my-stats?hours=72");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<object>();
        stats.Should().NotBeNull();
    }
    [Fact]
    public async Task GetMyStatistics_WithCustomHours_ShouldReturn400BadRequest()
    {
        // Arrange
        var userId = await GetCuidadorUserId();


        // Seed data to avoid NotFound
        var request = new
        {
            cuidadorId = userId,
            patientName = "Test Patient",
            dominantEmotion = "happy",
            emotionsDetected = new Dictionary<string, double> { { "happy", 0.9 } },
            timestamp = DateTime.UtcNow
        };
        await _client.PostAsJsonAsync("/api/history/cuidador-emotion", request);

        // Act
        var response = await _client.GetAsync("/api/history/statistics/my-stats?hours=12");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    [Fact]
    public async Task GetMyStatistics_WithCustomHours_ShouldReturn404NotFound()
    {
        // Arrange
        await GetCuidadorUserId();


        // Act
        var response = await _client.GetAsync("/api/history/statistics/my-stats?hours=24");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserStatistics_AsOwner_ShouldReturn200OK()
    {
        // Arrange
        var userId = await GetCuidadorUserId();


        // Seed data
        var request = new
        {
            cuidadorId = userId,
            patientName = "Test Patient",
            dominantEmotion = "happy",
            emotionsDetected = new Dictionary<string, double> { { "happy", 0.9 } },
            timestamp = DateTime.UtcNow
        };
        await _client.PostAsJsonAsync("/api/history/cuidador-emotion", request);

        // Act
        var response = await _client.GetAsync($"/api/history/statistics/user/{userId}?hours=24");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<object>();
        stats.Should().NotBeNull();
    }

    // Teste de admin removido

    [Fact]
    public async Task GetUserStatistics_AsNonOwner_ShouldReturn400BadRequest()
    {
        // Arrange
        var userId1 = await GetCuidadorUserId();
        var client2 = _factory.CreateClientWithCookies();
        var userId2 = await GetCuidadorUserId(client2);
        


        // Act - Try to access another user's statistics
        var response = await _client.GetAsync($"/api/history/statistics/user/{userId2}?hours=24");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // Rotas POST /api/history não existem - usar POST /api/history/cuidador-emotion

    [Fact]
    public async Task GetHistoryByFilter_WithValidFilter_ShouldReturn200OK()
    {
        // Arrange
        var userId = await GetCuidadorUserId();


        var filter = new
        {
            userId = userId,
            startDate = DateTime.UtcNow.AddDays(-7),
            endDate = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/history/filter", filter);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await response.Content.ReadFromJsonAsync<List<HistoryRecord>>();
        history.Should().NotBeNull();
    }

    [Fact]
    public async Task GetHistoryByFilter_WithoutAuth_ShouldReturn401Unauthorized()
    {
        // Arrange
        var filter = new
        {
            userId = 1,
            startDate = DateTime.UtcNow.AddDays(-7),
            endDate = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/history/filter", filter);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Teste de cleanup requer admin - removido por enquanto

    [Fact]
    public async Task ClearOldHistory_AsCuidador_ShouldReturn403Forbidden()
    {
        // Arrange
        await GetCuidadorUserId();


        // Act
        var response = await _client.DeleteAsync("/api/history/cleanup?days=90");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // Rotas GET /api/history/trends/user/{userId} não existem

    [Fact]
    public async Task SaveCuidadorEmotion_ValidRequest_ShouldReturn200OK()
    {
        // Arrange
        var userId = await GetCuidadorUserId();


        var request = new
        {
            cuidadorId = userId,
            patientName = "Test Patient",
            dominantEmotion = "happy",
            emotionsDetected = new Dictionary<string, double> { { "happy", 0.9 } },
            timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/history/cuidador-emotion", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SaveCuidadorEmotion_InvalidRequest_ShouldReturn400BadRequest()
    {
        // Arrange
        await GetCuidadorUserId();


        var request = new
        {
            cuidadorId = 0 // Invalid ID
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/history/cuidador-emotion", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveCuidadorEmotion_AsOtherUser_ShouldReturn403Forbidden()
    {
        // Arrange
        var userId = await GetCuidadorUserId();


        var request = new
        {
            cuidadorId = userId + 1, // Trying to save for another user
            patientName = "Test Patient",
            dominantEmotion = "happy",
            emotionsDetected = new Dictionary<string, double> { { "happy", 0.9 } }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/history/cuidador-emotion", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
