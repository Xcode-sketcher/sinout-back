using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using APISinout.Models;
using APISinout.Tests.Fixtures;

namespace APISinout.Tests.Integration.Controllers;


public class PatientControllerIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PatientControllerIntegrationTests(TestWebApplicationFactory factory)
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
            Phone = "+55 11 99999-1002",
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
    public async Task CreatePatient_AsCuidador_ShouldReturn201Created()
    {
        // Arrange
        var userId = await GetCuidadorUserId();


        var request = new PatientRequest
        {
            Name = "New Patient",
            CuidadorId = userId
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/patients", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var patient = await response.Content.ReadFromJsonAsync<PatientResponse>();
        patient.Should().NotBeNull();
        patient!.Name.Should().Be("New Patient");
        patient.CuidadorId.Should().Be(userId);
    }

    // Teste de admin removido

    [Fact]
    public async Task CreatePatient_WithoutAuth_ShouldReturn401Unauthorized()
    {
        // Arrange
        var request = new PatientRequest
        {
            Name = "Unauthorized Patient",
            CuidadorId = "1"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/patients", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPatients_AsCuidador_ShouldReturnOnlyOwnPatients()
    {
        // Arrange
        var userId = await GetCuidadorUserId();


        // Create a patient for this cuidador
        var createRequest = new PatientRequest
        {
            Name = "Own Patient",
            CuidadorId = userId
        };
        await _client.PostAsJsonAsync("/api/patients", createRequest);

        // Act
        var response = await _client.GetAsync("/api/patients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var patients = await response.Content.ReadFromJsonAsync<List<PatientResponse>>();
        patients.Should().NotBeNull();
        patients.Should().AllSatisfy(p => p.CuidadorId.Should().Be(userId));
    }

    // Teste de admin removido

    [Fact]
    public async Task GetPatients_WithoutAuth_ShouldReturn401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/patients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPatientById_AsOwner_ShouldReturn200OK()
    {
        // Arrange
        var userId = await GetCuidadorUserId();


        // Create a patient
        var createRequest = new PatientRequest
        {
            Name = "Patient To Get",
            CuidadorId = userId
        };
        var createResponse = await _client.PostAsJsonAsync("/api/patients", createRequest);
        var createdPatient = await createResponse.Content.ReadFromJsonAsync<PatientResponse>();

        // Act
        var response = await _client.GetAsync($"/api/patients/{createdPatient!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var patient = await response.Content.ReadFromJsonAsync<PatientResponse>();
        patient.Should().NotBeNull();
        patient!.Id.Should().Be(createdPatient.Id);
    }

    [Fact]
    public async Task GetPatientById_AsNonOwner_ShouldReturn404NotFound()
    {
        // Arrange
        var userId1 = await GetCuidadorUserId();
        var client2 = _factory.CreateClientWithCookies();
        var userId2 = await GetCuidadorUserId(client2);
        
        // Cuidador 1 creates a patient

        var createRequest = new PatientRequest
        {
            Name = "Private Patient",
            CuidadorId = userId1
        };
        var createResponse = await _client.PostAsJsonAsync("/api/patients", createRequest);
        var createdPatient = await createResponse.Content.ReadFromJsonAsync<PatientResponse>();

        // Cuidador 2 tries to access it


        // Act
        var response = await client2.GetAsync($"/api/patients/{createdPatient!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Teste de admin removido

    [Fact]
    public async Task UpdatePatient_AsOwner_ShouldReturn200OK()
    {
        // Arrange
        var userId = await GetCuidadorUserId();


        // Create a patient
        var createRequest = new PatientRequest
        {
            Name = "Patient To Update",
            CuidadorId = userId
        };
        var createResponse = await _client.PostAsJsonAsync("/api/patients", createRequest);
        var createdPatient = await createResponse.Content.ReadFromJsonAsync<PatientResponse>();

        // Update it
        var updateRequest = new PatientRequest
        {
            Name = "Updated Patient Name",
            CuidadorId = userId
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/patients/{createdPatient!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<PatientResponse>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Patient Name");
    }

    [Fact]
    public async Task UpdatePatient_AsNonOwner_ShouldReturn400BadRequest()
    {
        // Arrange
        var userId1 = await GetCuidadorUserId();
        var client2 = _factory.CreateClientWithCookies();
        var userId2 = await GetCuidadorUserId(client2);
        
        // Cuidador 1 creates a patient

        var createRequest = new PatientRequest
        {
            Name = "Patient To Update",
            CuidadorId = userId1
        };
        var createResponse = await _client.PostAsJsonAsync("/api/patients", createRequest);
        var createdPatient = await createResponse.Content.ReadFromJsonAsync<PatientResponse>();

        // Cuidador 2 tries to update it

        var updateRequest = new PatientRequest
        {
            Name = "Hacked Name",
            CuidadorId = userId2
        };

        // Act
        var response = await client2.PutAsJsonAsync($"/api/patients/{createdPatient!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeletePatient_AsOwner_ShouldReturn200OK()
    {
        // Arrange
        var userId = await GetCuidadorUserId();


        // Create a patient
        var createRequest = new PatientRequest
        {
            Name = "Patient To Delete",
            CuidadorId = userId
        };
        var createResponse = await _client.PostAsJsonAsync("/api/patients", createRequest);
        var createdPatient = await createResponse.Content.ReadFromJsonAsync<PatientResponse>();

        // Act
        var response = await _client.DeleteAsync($"/api/patients/{createdPatient!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeletePatient_AsNonOwner_ShouldReturn400BadRequest()
    {
        // Arrange
        var userId1 = await GetCuidadorUserId();
        var client2 = _factory.CreateClientWithCookies();
        var userId2 = await GetCuidadorUserId(client2);
        
        // Cuidador 1 creates a patient

        var createRequest = new PatientRequest
        {
            Name = "Patient To Delete",
            CuidadorId = userId1
        };
        var createResponse = await _client.PostAsJsonAsync("/api/patients", createRequest);
        var createdPatient = await createResponse.Content.ReadFromJsonAsync<PatientResponse>();

        // Cuidador 2 tries to delete it


        // Act
        var response = await client2.DeleteAsync($"/api/patients/{createdPatient!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // Teste de admin removido
}
