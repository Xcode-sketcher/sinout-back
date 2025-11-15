using APISinout.Models;

namespace APISinout.Tests.Fixtures;

public static class PatientFixtures
{
    public static Patient CreateValidPatient(int id = 1, int caregiverId = 1)
    {
        return new Patient
        {
            Id = id,
            Name = "Maria Silva",
            CaregiverId = caregiverId,
            DataCadastro = DateTime.UtcNow,
            Status = true,
            AdditionalInfo = "Paciente com ELA",
            ProfilePhoto = null,
            CreatedBy = "test"
        };
    }

    public static PatientRequest CreateValidPatientRequest(int? caregiverId = null)
    {
        return new PatientRequest
        {
            Name = "Maria Silva",
            CaregiverId = caregiverId,
            AdditionalInfo = "Paciente com ELA",
            ProfilePhoto = null
        };
    }

    public static List<Patient> CreateMultiplePatients(int caregiverId, int count = 3)
    {
        var patients = new List<Patient>();
        for (int i = 1; i <= count; i++)
        {
            var patient = CreateValidPatient(i, caregiverId);
            patient.Name = $"Paciente {i}";
            patients.Add(patient);
        }
        return patients;
    }
}
