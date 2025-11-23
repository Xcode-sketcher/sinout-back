using APISinout.Models;
using MongoDB.Bson;

namespace APISinout.Tests.Fixtures;

public static class PatientFixtures
{
    public static Patient CreateValidPatient(string? id = null, string? cuidadorId = null)
    {
        return new Patient
        {
            Id = id ?? ObjectId.GenerateNewId().ToString(),
            Name = "Maria Silva",
            CuidadorId = cuidadorId ?? ObjectId.GenerateNewId().ToString(),
            DataCadastro = DateTime.UtcNow,
            AdditionalInfo = "Paciente com ELA",
            ProfilePhoto = null
        };
    }

    public static PatientRequest CreateValidPatientRequest(string? cuidadorId = null)
    {
        return new PatientRequest
        {
            Name = "Maria Silva",
            CuidadorId = cuidadorId,
            AdditionalInfo = "Paciente com ELA",
            ProfilePhoto = null
        };
    }

    public static List<Patient> CreateMultiplePatients(string cuidadorId, int count = 3)
    {
        var patients = new List<Patient>();
        for (int i = 1; i <= count; i++)
        {
            var patient = CreateValidPatient(ObjectId.GenerateNewId().ToString(), cuidadorId);
            patient.Name = $"Paciente {i}";
            patients.Add(patient);
        }
        return patients;
    }
}
