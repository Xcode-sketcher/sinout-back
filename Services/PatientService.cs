// --- SERVIÇO DE PACIENTES: LÓGICA DE NEGÓCIO PARA PACIENTES ---
// Gerencia todas as operações relacionadas a pacientes com validações de segurança

using APISinout.Models;
using APISinout.Data;
using APISinout.Helpers;

namespace APISinout.Services;

public interface IPatientService
{
    Task<PatientResponse> CreatePatientAsync(PatientRequest request, int currentUserId, string currentUserRole);
    Task<PatientResponse> GetPatientByIdAsync(int id, int currentUserId, string currentUserRole);
    Task<List<PatientResponse>> GetPatientsByCaregiverAsync(int caregiverId);
    Task<List<PatientResponse>> GetAllPatientsAsync(); // Apenas Admin
    Task<PatientResponse> UpdatePatientAsync(int id, PatientRequest request, int currentUserId, string currentUserRole);
    Task DeletePatientAsync(int id, int currentUserId, string currentUserRole);
}

public class PatientService : IPatientService
{
    private readonly IPatientRepository _patientRepository;
    private readonly IUserRepository _userRepository;

    public PatientService(IPatientRepository patientRepository, IUserRepository userRepository)
    {
        _patientRepository = patientRepository;
        _userRepository = userRepository;
    }

    public async Task<PatientResponse> CreatePatientAsync(PatientRequest request, int currentUserId, string currentUserRole)
    {
        if (string.IsNullOrEmpty(request.Name))
            throw new AppException("Nome do paciente é obrigatório");

        // Definir o cuidador
        int caregiverId;
        string createdBy;

        if (currentUserRole == UserRole.Admin.ToString())
        {
            // Admin pode criar paciente para qualquer cuidador
            if (!request.CaregiverId.HasValue)
                throw new AppException("Administrador deve especificar o cuidador");

            var caregiver = await _userRepository.GetByIdAsync(request.CaregiverId.Value);
            if (caregiver == null || caregiver.Role != UserRole.Caregiver.ToString())
                throw new AppException("Cuidador inválido");

            caregiverId = request.CaregiverId.Value;
            createdBy = $"admin_{currentUserId}";
        }
        else if (currentUserRole == UserRole.Caregiver.ToString())
        {
            // Caregiver só pode criar para si mesmo
            caregiverId = currentUserId;
            createdBy = "self";
        }
        else
        {
            throw new AppException($"Apenas {UserRole.Admin} e {UserRole.Caregiver} podem cadastrar pacientes");
        }

        var patient = new Patient
        {
            Id = await _patientRepository.GetNextPatientIdAsync(),
            Name = request.Name.Trim(),
            CaregiverId = caregiverId,
            DataCadastro = DateTime.UtcNow,
            Status = true,
            AdditionalInfo = request.AdditionalInfo?.Trim(),
            ProfilePhoto = request.ProfilePhoto,
            CreatedBy = createdBy
        };

        await _patientRepository.CreatePatientAsync(patient);

        var caregiverUser = await _userRepository.GetByIdAsync(caregiverId);
        return new PatientResponse(patient, caregiverUser?.Name);
    }

    public async Task<PatientResponse> GetPatientByIdAsync(int id, int currentUserId, string currentUserRole)
    {
        var patient = await _patientRepository.GetByIdAsync(id);
        if (patient == null)
            throw new AppException("Paciente não encontrado");

        // Verificar permissão
        if (currentUserRole != UserRole.Admin.ToString() && patient.CaregiverId != currentUserId)
            throw new AppException("Acesso negado");

        var caregiver = await _userRepository.GetByIdAsync(patient.CaregiverId);
        return new PatientResponse(patient, caregiver?.Name);
    }

    public async Task<List<PatientResponse>> GetPatientsByCaregiverAsync(int caregiverId)
    {
        var patients = await _patientRepository.GetByCaregiverIdAsync(caregiverId);
        var caregiver = await _userRepository.GetByIdAsync(caregiverId);

        return patients.Select(p => new PatientResponse(p, caregiver?.Name)).ToList();
    }

    public async Task<List<PatientResponse>> GetAllPatientsAsync()
    {
        var patients = await _patientRepository.GetAllAsync();
        var responses = new List<PatientResponse>();

        foreach (var patient in patients)
        {
            var caregiver = await _userRepository.GetByIdAsync(patient.CaregiverId);
            responses.Add(new PatientResponse(patient, caregiver?.Name));
        }

        return responses;
    }

    public async Task<PatientResponse> UpdatePatientAsync(int id, PatientRequest request, int currentUserId, string currentUserRole)
    {
        var patient = await _patientRepository.GetByIdAsync(id);
        if (patient == null)
            throw new AppException("Paciente não encontrado");

        // Verificar permissão
        if (currentUserRole != UserRole.Admin.ToString() && patient.CaregiverId != currentUserId)
            throw new AppException("Acesso negado");

        if (!string.IsNullOrEmpty(request.Name))
            patient.Name = request.Name.Trim();

        if (!string.IsNullOrEmpty(request.AdditionalInfo))
            patient.AdditionalInfo = request.AdditionalInfo.Trim();

        if (request.ProfilePhoto != null)
            patient.ProfilePhoto = request.ProfilePhoto;

        // Apenas Admin pode mudar o cuidador
        if (request.CaregiverId.HasValue && currentUserRole == UserRole.Admin.ToString())
        {
            var newCaregiver = await _userRepository.GetByIdAsync(request.CaregiverId.Value);
            if (newCaregiver == null || newCaregiver.Role != UserRole.Caregiver.ToString())
                throw new AppException("Cuidador inválido");
            
            patient.CaregiverId = request.CaregiverId.Value;
        }

        await _patientRepository.UpdatePatientAsync(id, patient);

        var caregiver = await _userRepository.GetByIdAsync(patient.CaregiverId);
        return new PatientResponse(patient, caregiver?.Name);
    }

    public async Task DeletePatientAsync(int id, int currentUserId, string currentUserRole)
    {
        var patient = await _patientRepository.GetByIdAsync(id);
        if (patient == null)
            throw new AppException("Paciente não encontrado");

        // Verificar permissão
        if (currentUserRole != UserRole.Admin.ToString() && patient.CaregiverId != currentUserId)
            throw new AppException("Acesso negado");

        await _patientRepository.DeletePatientAsync(id);
    }
}
