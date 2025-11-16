using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using APISinout.Models;
using APISinout.Tests.Fixtures;

namespace APISinout.Tests.Integration.Controllers;

/// <summary>
/// Testes de integração para PatientController
/// Testa operações CRUD de pacientes com validação de permissões
/// </summary>
public class PatientControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PatientControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string token, int userId)> GetCaregiverTokenAndId()
    {
        var caregiverEmail = $"caregiver{Guid.NewGuid()}@test.com";
        var caregiverPassword = "Caregiver@123";
        
        var registerRequest = new RegisterRequest
        {
            Name = "Caregiver User",
            Email = caregiverEmail,
            Password = caregiverPassword,
            Phone = "+55 11 99999-1002",
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
        return (authResponse!.Token, authResponse.User.UserId);
    }

    [Fact]
    public async Task CreatePatient_AsCaregiver_ShouldReturn201Created()
    {
        // Arrange
        var (token, userId) = await GetCaregiverTokenAndId();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new PatientRequest
        {
            Name = "New Patient",
            CaregiverId = userId
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/patients", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var patient = await response.Content.ReadFromJsonAsync<PatientResponse>();
        patient.Should().NotBeNull();
        patient!.Name.Should().Be("New Patient");
        patient.CaregiverId.Should().Be(userId);
    }

    // Teste de admin removido

    [Fact]
    public async Task CreatePatient_WithoutAuth_ShouldReturn401Unauthorized()
    {
        // Arrange
        var request = new PatientRequest
        {
            Name = "Unauthorized Patient",
            CaregiverId = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/patients", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPatients_AsCaregiver_ShouldReturnOnlyOwnPatients()
    {
        // Arrange
        var (token, userId) = await GetCaregiverTokenAndId();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a patient for this caregiver
        var createRequest = new PatientRequest
        {
            Name = "Own Patient",
            CaregiverId = userId
        };
        await _client.PostAsJsonAsync("/api/patients", createRequest);

        // Act
        var response = await _client.GetAsync("/api/patients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var patients = await response.Content.ReadFromJsonAsync<List<PatientResponse>>();
        patients.Should().NotBeNull();
        patients.Should().AllSatisfy(p => p.CaregiverId.Should().Be(userId));
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
        var (token, userId) = await GetCaregiverTokenAndId();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a patient
        var createRequest = new PatientRequest
        {
            Name = "Patient To Get",
            CaregiverId = userId
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
        var (token1, userId1) = await GetCaregiverTokenAndId();
        var (token2, _) = await GetCaregiverTokenAndId();
        
        // Caregiver 1 creates a patient
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        var createRequest = new PatientRequest
        {
            Name = "Private Patient",
            CaregiverId = userId1
        };
        var createResponse = await _client.PostAsJsonAsync("/api/patients", createRequest);
        var createdPatient = await createResponse.Content.ReadFromJsonAsync<PatientResponse>();

        // Caregiver 2 tries to access it
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);

        // Act
        var response = await _client.GetAsync($"/api/patients/{createdPatient!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Teste de admin removido

    [Fact]
    public async Task GetPatientsByCaregiver_AsCaregiver_ShouldReturn403Forbidden()
    {
        // Arrange
        var (token, _) = await GetCaregiverTokenAndId();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/patients/caregiver/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdatePatient_AsOwner_ShouldReturn200OK()
    {
        // Arrange
        var (token, userId) = await GetCaregiverTokenAndId();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a patient
        var createRequest = new PatientRequest
        {
            Name = "Patient To Update",
            CaregiverId = userId
        };
        var createResponse = await _client.PostAsJsonAsync("/api/patients", createRequest);
        var createdPatient = await createResponse.Content.ReadFromJsonAsync<PatientResponse>();

        // Update it
        var updateRequest = new PatientRequest
        {
            Name = "Updated Patient Name",
            CaregiverId = userId
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
        var (token1, userId1) = await GetCaregiverTokenAndId();
        var (token2, userId2) = await GetCaregiverTokenAndId();
        
        // Caregiver 1 creates a patient
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        var createRequest = new PatientRequest
        {
            Name = "Patient To Update",
            CaregiverId = userId1
        };
        var createResponse = await _client.PostAsJsonAsync("/api/patients", createRequest);
        var createdPatient = await createResponse.Content.ReadFromJsonAsync<PatientResponse>();

        // Caregiver 2 tries to update it
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);
        var updateRequest = new PatientRequest
        {
            Name = "Hacked Name",
            CaregiverId = userId2
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/patients/{createdPatient!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeletePatient_AsOwner_ShouldReturn200OK()
    {
        // Arrange
        var (token, userId) = await GetCaregiverTokenAndId();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a patient
        var createRequest = new PatientRequest
        {
            Name = "Patient To Delete",
            CaregiverId = userId
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
        var (token1, userId1) = await GetCaregiverTokenAndId();
        var (token2, _) = await GetCaregiverTokenAndId();
        
        // Caregiver 1 creates a patient
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        var createRequest = new PatientRequest
        {
            Name = "Patient To Delete",
            CaregiverId = userId1
        };
        var createResponse = await _client.PostAsJsonAsync("/api/patients", createRequest);
        var createdPatient = await createResponse.Content.ReadFromJsonAsync<PatientResponse>();

        // Caregiver 2 tries to delete it
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);

        // Act
        var response = await _client.DeleteAsync($"/api/patients/{createdPatient!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // Teste de admin removido
}
