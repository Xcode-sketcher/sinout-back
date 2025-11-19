using APISinout.Models;

namespace APISinout.Tests.Fixtures;

public static class PatientFixtures
{
    public static Patient CreateValidPatient(int id = 1, int cuidadorId = 1)
    {
        return new Patient
        {
            Id = id,
            Name = "Maria Silva",
            CuidadorId = cuidadorId,
            DataCadastro = DateTime.UtcNow,
            Status = true,
            AdditionalInfo = "Paciente com ELA",
            ProfilePhoto = null,
            CreatedBy = "test"
        };
    }

    public static PatientRequest CreateValidPatientRequest(int? cuidadorId = null)
    {
        return new PatientRequest
        {
            Name = "Maria Silva",
            CuidadorId = cuidadorId,
            AdditionalInfo = "Paciente com ELA",
            ProfilePhoto = null
        };
    }

    public static List<Patient> CreateMultiplePatients(int cuidadorId, int count = 3)
    {
        var patients = new List<Patient>();
        for (int i = 1; i <= count; i++)
        {
            var patient = CreateValidPatient(i, cuidadorId);
            patient.Name = $"Paciente {i}";
            patients.Add(patient);
        }
        return patients;
    }
}
