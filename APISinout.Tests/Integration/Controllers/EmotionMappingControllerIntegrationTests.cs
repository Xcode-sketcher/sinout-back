using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using APISinout.Models;
using APISinout.Tests.Fixtures;

namespace APISinout.Tests.Integration.Controllers;


public class EmotionMappingControllerIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EmotionMappingControllerIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetCuidadorUserId(HttpClient? client = null)
    {
        var httpClient = client ?? _client;
        var cuidadorEmail = $"cuidador{Guid.NewGuid()}@test.com";
        var cuidadorPassword = "Cuidador@123";
        
        var registerRequest = new RegisterRequest
        {
            Name = "Cuidador User",
            Email = cuidadorEmail,
            Password = cuidadorPassword,
            Phone = "+55 11 99999-3002",
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
        return userResponse!.UserId!;
    }

    [Fact]
    public async Task CreateMapping_WithValidData_ShouldReturn201Created()
    {
        // Arrange
        var userId = await GetCuidadorUserId();


        var request = new EmotionMappingRequest
        {
            UserId = userId,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Quero água",
            Priority = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/emotion-mappings", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var mapping = await response.Content.ReadFromJsonAsync<EmotionMappingResponse>();
        mapping.Should().NotBeNull();
        (mapping?.Emotion ?? throw new InvalidOperationException("Emotion not found")).Should().Be("happy");
        mapping.Message.Should().Be("Quero água");
    }

    [Fact]
    public async Task CreateMapping_WithoutUserId_ShouldUseCurrentUser()
    {
        // Arrange
        await GetCuidadorUserId();


        var request = new EmotionMappingRequest
        {
            Emotion = "sad",
            IntensityLevel = "moderate",
            MinPercentage = 70.0,
            Message = "Preciso de ajuda",
            Priority = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/emotion-mappings", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var mapping = await response.Content.ReadFromJsonAsync<EmotionMappingResponse>();
        mapping.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateMapping_WithoutAuth_ShouldReturn401Unauthorized()
    {
        // Arrange
        var request = new EmotionMappingRequest
        {
            UserId = "1",
            Emotion = "angry",
            IntensityLevel = "high",
            MinPercentage = 75.0,
            Message = "Estou com raiva",
            Priority = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/emotion-mappings", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateMapping_ExceedingLimit_ShouldReturn400BadRequest()
    {
        // Arrange
        var userId = await GetCuidadorUserId();


        // Create first mapping
        var request1 = new EmotionMappingRequest
        {
            UserId = userId,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Primeira mensagem",
            Priority = 1
        };
        await _client.PostAsJsonAsync("/api/emotion-mappings", request1);

        // Create second mapping (should succeed - max is 2)
        var request2 = new EmotionMappingRequest
        {
            UserId = userId,
            Emotion = "happy",
            IntensityLevel = "moderate",
            MinPercentage = 85.0,
            Message = "Segunda mensagem",
            Priority = 2
        };
        await _client.PostAsJsonAsync("/api/emotion-mappings", request2);

        // Try to create third mapping (should fail)
        var request3 = new EmotionMappingRequest
        {
            UserId = userId,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 90.0,
            Message = "Terceira mensagem",
            Priority = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/emotion-mappings", request3);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMyMappings_WithValidToken_ShouldReturn200OK()
    {
        // Arrange
        var userId = await GetCuidadorUserId();


        // Create a mapping first
        var createRequest = new EmotionMappingRequest
        {
            UserId = userId,
            Emotion = "neutral",
            IntensityLevel = "moderate",
            MinPercentage = 50.0,
            Message = "Tudo bem",
            Priority = 1
        };
        await _client.PostAsJsonAsync("/api/emotion-mappings", createRequest);

        // Act
        var response = await _client.GetAsync("/api/emotion-mappings/my-rules");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var mappings = await response.Content.ReadFromJsonAsync<List<EmotionMappingResponse>>();
        mappings.Should().NotBeNull();
        mappings.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMyMappings_WithoutAuth_ShouldReturn401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/emotion-mappings/my-rules");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMappingsByUser_AsOwner_ShouldReturn200OK()
    {
        // Arrange
        var userId = await GetCuidadorUserId();


        // Create a mapping first
        var createRequest = new EmotionMappingRequest
        {
            UserId = userId,
            Emotion = "fear",
            IntensityLevel = "high",
            MinPercentage = 60.0,
            Message = "Estou com medo",
            Priority = 1
        };
        await _client.PostAsJsonAsync("/api/emotion-mappings", createRequest);

        // Act
        var response = await _client.GetAsync($"/api/emotion-mappings/user/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var mappings = await response.Content.ReadFromJsonAsync<List<EmotionMappingResponse>>();
        mappings.Should().NotBeNull();
    }

    // Teste de admin removido

    [Fact]
    public async Task GetMappingsByUser_AsNonOwner_ShouldReturn400BadRequest()
    {
        // Arrange
        var client1 = _factory.CreateClientWithCookies();
        var userId1 = await GetCuidadorUserId(client1);
        
        var client2 = _factory.CreateClientWithCookies();
        var userId2 = await GetCuidadorUserId(client2);
        


        // Act - Try to access another user's mappings using client2
        var response = await client2.GetAsync($"/api/emotion-mappings/user/{userId1}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateMapping_AsOwner_ShouldReturn200OK()
    {
        // Arrange
        var userId = await GetCuidadorUserId();


        // Create a mapping
        var createRequest = new EmotionMappingRequest
        {
            UserId = userId,
            Emotion = "disgust",
            IntensityLevel = "moderate",
            MinPercentage = 70.0,
            Message = "Não gosto disso",
            Priority = 1
        };
        var createResponse = await _client.PostAsJsonAsync("/api/emotion-mappings", createRequest);
        var createdMapping = await createResponse.Content.ReadFromJsonAsync<EmotionMappingResponse>();

        // Update it
        var updateRequest = new EmotionMappingRequest
        {
            UserId = userId,
            Emotion = "disgust",
            IntensityLevel = "high",
            MinPercentage = 75.0,
            Message = "Realmente não gosto",
            Priority = 1
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/emotion-mappings/{createdMapping?.Id ?? throw new InvalidOperationException("Id not found")}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<EmotionMappingResponse>();
        updated.Should().NotBeNull();
        (updated?.Message ?? throw new InvalidOperationException("Message not found")).Should().Be("Realmente não gosto");
    }

    [Fact]
    public async Task UpdateMapping_AsNonOwner_ShouldReturn400BadRequest()
    {
        // Arrange
        var client1 = _factory.CreateClientWithCookies();
        var userId1 = await GetCuidadorUserId(client1);
        
        var client2 = _factory.CreateClientWithCookies();
        var userId2 = await GetCuidadorUserId(client2);
        
        // Cuidador 1 creates a mapping

        var createRequest = new EmotionMappingRequest
        {
            UserId = userId1,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Estou feliz",
            Priority = 1
        };
        var createResponse = await client1.PostAsJsonAsync("/api/emotion-mappings", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdMapping = await createResponse.Content.ReadFromJsonAsync<EmotionMappingResponse>();
        createdMapping.Should().NotBeNull();

        // Cuidador 2 tries to update it

        var updateRequest = new EmotionMappingRequest
        {
            UserId = userId1,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 85.0,
            Message = "Hacked message",
            Priority = 1
        };

        // Act
        var response = await client2.PutAsJsonAsync($"/api/emotion-mappings/{createdMapping?.Id ?? throw new InvalidOperationException("Id not found")}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteMapping_AsOwner_ShouldReturn200OK()
    {
        // Arrange
        var userId = await GetCuidadorUserId();


        // Create a mapping
        var createRequest = new EmotionMappingRequest
        {
            UserId = userId,
            Emotion = "angry",
            IntensityLevel = "moderate",
            MinPercentage = 60.0,
            Message = "Desprezo",
            Priority = 1
        };
        var createResponse = await _client.PostAsJsonAsync("/api/emotion-mappings", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdMapping = await createResponse.Content.ReadFromJsonAsync<EmotionMappingResponse>();
        createdMapping.Should().NotBeNull();

        // Act
        var response = await _client.DeleteAsync($"/api/emotion-mappings/{createdMapping?.Id ?? throw new InvalidOperationException("Id not found")}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteMapping_AsNonOwner_ShouldReturn400BadRequest()
    {
        // Arrange
        var client1 = _factory.CreateClientWithCookies();
        var userId1 = await GetCuidadorUserId(client1);
        
        var client2 = _factory.CreateClientWithCookies();
        var userId2 = await GetCuidadorUserId(client2);
        
        // Cuidador 1 creates a mapping

        var createRequest = new EmotionMappingRequest
        {
            UserId = userId1,
            Emotion = "neutral",
            IntensityLevel = "moderate",
            MinPercentage = 50.0,
            Message = "Calmo",
            Priority = 1
        };
        var createResponse = await client1.PostAsJsonAsync("/api/emotion-mappings", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdMapping = await createResponse.Content.ReadFromJsonAsync<EmotionMappingResponse>();
        createdMapping.Should().NotBeNull();

        // Cuidador 2 tries to delete it


        // Act
        var response = await client2.DeleteAsync($"/api/emotion-mappings/{createdMapping?.Id ?? throw new InvalidOperationException("Id not found")}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // Teste de admin removido

    [Fact]
    public async Task CreateMapping_WithInvalidPriority_ShouldReturn400BadRequest()
    {
        // Arrange
        var userId = await GetCuidadorUserId();


        var request = new EmotionMappingRequest
        {
            UserId = userId,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Mensagem",
            Priority = 3 // Invalid - only 1 or 2 allowed
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/emotion-mappings", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateMapping_WithInvalidPercentage_ShouldReturn400BadRequest()
    {
        // Arrange
        var userId = await GetCuidadorUserId();


        var request = new EmotionMappingRequest
        {
            UserId = userId,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 150.0, // Invalid - must be 0-100
            Message = "Mensagem",
            Priority = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/emotion-mappings", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
