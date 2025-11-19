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
    Task<List<PatientResponse>> GetPatientsByCuidadorAsync(int cuidadorId);
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
        int cuidadorId;
        string createdBy;

        if (currentUserRole == UserRole.Admin.ToString())
        {
            // Admin pode criar paciente para qualquer cuidador
            if (!request.CuidadorId.HasValue)
                throw new AppException("Administrador deve especificar o cuidador");

            var cuidador = await _userRepository.GetByIdAsync(request.CuidadorId.Value);
            if (cuidador == null || cuidador.Role != UserRole.Cuidador.ToString())
                throw new AppException("Cuidador inválido");

            cuidadorId = request.CuidadorId.Value;
            createdBy = $"admin_{currentUserId}";
        }
        else if (currentUserRole == UserRole.Cuidador.ToString())
        {
            // Cuidador só pode criar para si mesmo
            cuidadorId = currentUserId;
            createdBy = "self";
        }
        else
        {
            throw new AppException($"Apenas {UserRole.Admin} e {UserRole.Cuidador} podem cadastrar pacientes");
        }

        var patient = new Patient
        {
            Id = await _patientRepository.GetNextPatientIdAsync(),
            Name = request.Name.Trim(),
            CuidadorId = cuidadorId,
            DataCadastro = DateTime.UtcNow,
            Status = true,
            AdditionalInfo = request.AdditionalInfo?.Trim(),
            ProfilePhoto = request.ProfilePhoto,
            CreatedBy = createdBy
        };

        await _patientRepository.CreatePatientAsync(patient);

        var cuidadorUser = await _userRepository.GetByIdAsync(cuidadorId);
        return new PatientResponse(patient, cuidadorUser?.Name);
    }

    public async Task<PatientResponse> GetPatientByIdAsync(int id, int currentUserId, string currentUserRole)
    {
        var patient = await _patientRepository.GetByIdAsync(id);
        if (patient == null)
            throw new AppException("Paciente não encontrado");

        // Verificar permissão
        if (currentUserRole != UserRole.Admin.ToString() && patient.CuidadorId != currentUserId)
            throw new AppException("Acesso negado");

        var cuidador = await _userRepository.GetByIdAsync(patient.CuidadorId);
        return new PatientResponse(patient, cuidador?.Name);
    }

    public async Task<List<PatientResponse>> GetPatientsByCuidadorAsync(int cuidadorId)
    {
        var patients = await _patientRepository.GetByCuidadorIdAsync(cuidadorId);
        var cuidador = await _userRepository.GetByIdAsync(cuidadorId);

        return patients.Select(p => new PatientResponse(p, cuidador?.Name)).ToList();
    }

    public async Task<List<PatientResponse>> GetAllPatientsAsync()
    {
        var patients = await _patientRepository.GetAllAsync();
        var responses = new List<PatientResponse>();

        foreach (var patient in patients)
        {
            var cuidador = await _userRepository.GetByIdAsync(patient.CuidadorId);
            responses.Add(new PatientResponse(patient, cuidador?.Name));
        }

        return responses;
    }

    public async Task<PatientResponse> UpdatePatientAsync(int id, PatientRequest request, int currentUserId, string currentUserRole)
    {
        var patient = await _patientRepository.GetByIdAsync(id);
        if (patient == null)
            throw new AppException("Paciente não encontrado");

        // Verificar permissão
        if (currentUserRole != UserRole.Admin.ToString() && patient.CuidadorId != currentUserId)
            throw new AppException("Acesso negado");

        if (!string.IsNullOrEmpty(request.Name))
            patient.Name = request.Name.Trim();

        if (!string.IsNullOrEmpty(request.AdditionalInfo))
            patient.AdditionalInfo = request.AdditionalInfo.Trim();

        if (request.ProfilePhoto != null)
            patient.ProfilePhoto = request.ProfilePhoto;

        // Apenas Admin pode mudar o cuidador
        if (request.CuidadorId.HasValue && currentUserRole == UserRole.Admin.ToString())
        {
            var newCuidador = await _userRepository.GetByIdAsync(request.CuidadorId.Value);
            if (newCuidador == null || newCuidador.Role != UserRole.Cuidador.ToString())
                throw new AppException("Cuidador inválido");
            
            patient.CuidadorId = request.CuidadorId.Value;
        }

        await _patientRepository.UpdatePatientAsync(id, patient);

        var cuidador = await _userRepository.GetByIdAsync(patient.CuidadorId);
        return new PatientResponse(patient, cuidador?.Name);
    }

    public async Task DeletePatientAsync(int id, int currentUserId, string currentUserRole)
    {
        var patient = await _patientRepository.GetByIdAsync(id);
        if (patient == null)
            throw new AppException("Paciente não encontrado");

        // Verificar permissão
        if (currentUserRole != UserRole.Admin.ToString() && patient.CuidadorId != currentUserId)
            throw new AppException("Acesso negado");

        await _patientRepository.DeletePatientAsync(id);
    }
}
