using APISinout.Models;
using APISinout.Data;
using APISinout.Helpers;
using Microsoft.Extensions.Logging;

namespace APISinout.Services;

public class PatientService : IPatientService
{
    private readonly IPatientRepository _patientRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<PatientService>? _logger;

    public PatientService(IPatientRepository patientRepository, IUserRepository userRepository, ILogger<PatientService>? logger = null)
    {
        _patientRepository = patientRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    // Obtém um paciente por ID.
    // Usuário só pode ver seus próprios pacientes.
    public async Task<PatientResponse> GetPatientByIdAsync(string id, string currentUserId, string currentUserRole)
    {
        var patient = await _patientRepository.GetByIdAsync(id);
        if (patient == null)
            throw new AppException("Paciente não encontrado");

        // Verificar se o paciente pertence ao usuário
        if (patient.CuidadorId != currentUserId && currentUserRole != "Admin")
            throw new AppException("Acesso negado");

        string? cuidadorName = null;
        if (!string.IsNullOrEmpty(patient.CuidadorId))
        {
            var cuidador = await _userRepository.GetByIdAsync(patient.CuidadorId);
            cuidadorName = cuidador?.Name;
        }

        return new PatientResponse(patient, cuidadorName);
    }

    // Obtém os pacientes de um cuidador.
    public async Task<List<PatientResponse>> GetPatientsByCuidadorAsync(string cuidadorId)
    {
        var patients = await _patientRepository.GetByCuidadorIdAsync(cuidadorId);
        var cuidador = await _userRepository.GetByIdAsync(cuidadorId);

        return patients.Select(p => new PatientResponse(p, cuidador?.Name)).ToList();
    }

    // Obtém todos os pacientes do cuidador atual.
    public async Task<List<PatientResponse>> GetAllPatientsAsync()
    {
        var patients = await _patientRepository.GetAllAsync();
        var responses = new List<PatientResponse>();

        foreach (var patient in patients)
        {
            string? cuidadorName = null;
            if (!string.IsNullOrEmpty(patient.CuidadorId))
            {
                var cuidador = await _userRepository.GetByIdAsync(patient.CuidadorId);
                cuidadorName = cuidador?.Name;
            }
            responses.Add(new PatientResponse(patient, cuidadorName));
        }

        return responses;
    }

    // Atualiza um paciente.
    // Usuário só pode atualizar seus próprios pacientes.
    public async Task<PatientResponse> UpdatePatientAsync(string id, PatientRequest request, string currentUserId, string currentUserRole)
    {
        var patient = await _patientRepository.GetByIdAsync(id);
        if (patient == null)
            throw new AppException("Paciente não encontrado");

        // Verificar se o paciente pertence ao usuário
        if (patient.CuidadorId != currentUserId && currentUserRole != "Admin")
            throw new AppException("Acesso negado");

        if (!string.IsNullOrEmpty(request.Name))
            patient.Name = request.Name.Trim();

        if (!string.IsNullOrEmpty(request.AdditionalInfo))
            patient.AdditionalInfo = request.AdditionalInfo.Trim();

        if (request.ProfilePhoto.HasValue)
            patient.ProfilePhoto = request.ProfilePhoto;

        if (currentUserRole == "Admin" && !string.IsNullOrEmpty(request.CuidadorId))
        {
             var newCuidador = await _userRepository.GetByIdAsync(request.CuidadorId);
             if (newCuidador == null)
                 throw new AppException("Cuidador inválido");
             
             patient.CuidadorId = request.CuidadorId;
        }

        await _patientRepository.UpdatePatientAsync(id, patient);
        _logger?.LogInformation("Patient updated: Id={PatientId}", id);

        string? cuidadorName = null;
        if (!string.IsNullOrEmpty(patient.CuidadorId))
        {
            var cuidador = await _userRepository.GetByIdAsync(patient.CuidadorId);
            cuidadorName = cuidador?.Name;
        }
        return new PatientResponse(patient, cuidadorName);
    }

    // Exclui um paciente.
    // Usuário só pode deletar seus próprios pacientes.
    public async Task DeletePatientAsync(string id, string currentUserId, string currentUserRole)
    {
        var patient = await _patientRepository.GetByIdAsync(id);
        if (patient == null)
            throw new AppException("Paciente não encontrado");

        // Verificar se o paciente pertence ao usuário
        if (patient.CuidadorId != currentUserId && currentUserRole != "Admin")
            throw new AppException("Acesso negado");

        await _patientRepository.DeletePatientAsync(id);
        _logger?.LogInformation("Patient deleted: Id={PatientId}", id);
    }

    // Cria um novo paciente.
    // Apenas cuidadores podem criar pacientes para si mesmos.
    // Admins podem criar pacientes para qualquer cuidador.
    public async Task<PatientResponse> CreatePatientAsync(PatientRequest request, string cuidadorId, string role)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppException("Nome do paciente é obrigatório");

        string targetCuidadorId = cuidadorId;

        if (role == "Admin")
        {
            if (string.IsNullOrEmpty(request.CuidadorId))
                throw new AppException("Administrador deve especificar o cuidador");

            targetCuidadorId = request.CuidadorId;

            // Verificar se o cuidador existe
            var cuidador = await _userRepository.GetByIdAsync(targetCuidadorId);
            if (cuidador == null)
                throw new AppException("Cuidador inválido");
        }
        else if (role != "Cuidador")
        {
            throw new AppException("Apenas Cuidadores podem cadastrar pacientes");
        }

        var patient = new Patient
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Name = request.Name.Trim(),
            CuidadorId = targetCuidadorId,
            DataCadastro = DateTime.UtcNow,
            AdditionalInfo = request.AdditionalInfo?.Trim(),
            ProfilePhoto = request.ProfilePhoto
        };

        await _patientRepository.CreatePatientAsync(patient);
        _logger?.LogInformation("Patient created: Id={PatientId}, CuidadorId={CuidadorId}", patient.Id, patient.CuidadorId);

        var targetCuidador = await _userRepository.GetByIdAsync(targetCuidadorId);
        return new PatientResponse(patient, targetCuidador?.Name);
    }
}
