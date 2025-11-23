using MongoDB.Driver;
using APISinout.Models;

namespace APISinout.Data;

// Interface para operações de repositório de pacientes.
public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(string id);
    Task<List<Patient>> GetByCuidadorIdAsync(string cuidadorId);
    Task<List<Patient>> GetAllAsync();
    Task CreatePatientAsync(Patient patient);
    Task UpdatePatientAsync(string id, Patient patient);
    Task DeletePatientAsync(string id);
    Task<bool> ExistsAsync(string id);
}

// Implementação do repositório de pacientes usando MongoDB.
public class PatientRepository : IPatientRepository
{
    private readonly IMongoCollection<Patient> _patients;

    // Construtor que injeta o contexto do MongoDB.
    public PatientRepository(MongoDbContext context)
    {
        _patients = context.Patients;
    }

    // Construtor para testes (injeta coleções diretamente).
    public PatientRepository(IMongoCollection<Patient> patientsCollection)
    {
        _patients = patientsCollection;
    }

    // Obtém paciente por ID.
    public async Task<Patient?> GetByIdAsync(string id)
    {
        return await _patients.Find(p => p.Id == id).FirstOrDefaultAsync();
    }

    // Obtém pacientes por ID do cuidador.
    public async Task<List<Patient>> GetByCuidadorIdAsync(string cuidadorId)
    {
        return await _patients.Find(p => p.CuidadorId == cuidadorId).ToListAsync();
    }

    // Lista todos os pacientes.
    public async Task<List<Patient>> GetAllAsync()
    {
        return await _patients.Find(_ => true).ToListAsync();
    }

    // Cria um novo paciente.
    public async Task CreatePatientAsync(Patient patient)
    {
        await _patients.InsertOneAsync(patient);
    }

    // Atualiza um paciente existente.
    public async Task UpdatePatientAsync(string id, Patient patient)
    {
        var filter = Builders<Patient>.Filter.Eq(p => p.Id, id);
        var update = Builders<Patient>.Update
            .Set(p => p.Name, patient.Name)
            .Set(p => p.CuidadorId, patient.CuidadorId)
            .Set(p => p.AdditionalInfo, patient.AdditionalInfo)
            .Set(p => p.ProfilePhoto, patient.ProfilePhoto);

        await _patients.UpdateOneAsync(filter, update);
    }

    // Remove um paciente.
    public async Task DeletePatientAsync(string id)
    {
        var filter = Builders<Patient>.Filter.Eq(p => p.Id, id);
        await _patients.DeleteOneAsync(filter);
    }

    // Verifica se o paciente existe.
    public async Task<bool> ExistsAsync(string id)
    {
        var count = await _patients.CountDocumentsAsync(p => p.Id == id);
        return count > 0;
    }
}
