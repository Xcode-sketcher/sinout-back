using APISinout.Models;

namespace APISinout.Services;

public interface IPatientService
{
    Task<PatientResponse> GetPatientByIdAsync(string id, string currentUserId, string currentUserRole);
    Task<List<PatientResponse>> GetPatientsByCuidadorAsync(string cuidadorId);
    Task<List<PatientResponse>> GetAllPatientsAsync();
    Task<PatientResponse> UpdatePatientAsync(string id, PatientRequest request, string currentUserId, string currentUserRole);
    Task DeletePatientAsync(string id, string currentUserId, string currentUserRole);
    Task<PatientResponse> CreatePatientAsync(PatientRequest request, string cuidadorId, string role);
}
