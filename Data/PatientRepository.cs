using MongoDB.Driver;
using APISinout.Models;

namespace APISinout.Data;

// Interface para operações de repositório de pacientes.
public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(int id);
    Task<List<Patient>> GetByCuidadorIdAsync(int cuidadorId);
    Task<List<Patient>> GetAllAsync();
    Task CreatePatientAsync(Patient patient);
    Task UpdatePatientAsync(int id, Patient patient);
    Task DeletePatientAsync(int id);
    Task<int> GetNextPatientIdAsync();
    Task<bool> ExistsAsync(int id);
}

// Implementação do repositório de pacientes usando MongoDB.
public class PatientRepository : IPatientRepository
{
    private readonly IMongoCollection<Patient> _patients;
    private readonly IMongoCollection<Counter> _counters;

    // Construtor que injeta o contexto do MongoDB.
    public PatientRepository(MongoDbContext context)
    {
        _patients = context.Patients;
        _counters = context.Counters;
    }

    // Obtém paciente por ID.
    public async Task<Patient?> GetByIdAsync(int id)
    {
        return await _patients.Find(p => p.Id == id).FirstOrDefaultAsync();
    }

    // Obtém pacientes por ID do cuidador.
    public async Task<List<Patient>> GetByCuidadorIdAsync(int cuidadorId)
    {
        return await _patients.Find(p => p.CuidadorId == cuidadorId && p.Status).ToListAsync();
    }

    // Lista todos os pacientes ativos.
    public async Task<List<Patient>> GetAllAsync()
    {
        return await _patients.Find(p => p.Status).ToListAsync();
    }

    // Cria um novo paciente.
    public async Task CreatePatientAsync(Patient patient)
    {
        await _patients.InsertOneAsync(patient);
    }

    // Atualiza um paciente existente.
    public async Task UpdatePatientAsync(int id, Patient patient)
    {
        var filter = Builders<Patient>.Filter.Eq(p => p.Id, id);
        var update = Builders<Patient>.Update
            .Set(p => p.Name, patient.Name)
            .Set(p => p.CuidadorId, patient.CuidadorId)
            .Set(p => p.Status, patient.Status)
            .Set(p => p.AdditionalInfo, patient.AdditionalInfo)
            .Set(p => p.ProfilePhoto, patient.ProfilePhoto);

        await _patients.UpdateOneAsync(filter, update);
    }

    // Remove um paciente (soft delete).
    public async Task DeletePatientAsync(int id)
    {
        // Soft delete: apenas marca como inativo
        var filter = Builders<Patient>.Filter.Eq(p => p.Id, id);
        var update = Builders<Patient>.Update.Set(p => p.Status, false);
        await _patients.UpdateOneAsync(filter, update);
    }

    // Obtém o próximo ID disponível para paciente.
    public async Task<int> GetNextPatientIdAsync()
    {
        var filter = Builders<Counter>.Filter.Eq(c => c.Id, "patient");
        var update = Builders<Counter>.Update.Inc(c => c.Seq, 1);
        var options = new FindOneAndUpdateOptions<Counter>
        {
            ReturnDocument = ReturnDocument.After,
            IsUpsert = true
        };

        var counter = await _counters.FindOneAndUpdateAsync(filter, update, options);
        return counter?.Seq ?? 1;
    }

    // Verifica se o paciente existe.
    public async Task<bool> ExistsAsync(int id)
    {
        var count = await _patients.CountDocumentsAsync(p => p.Id == id);
        return count > 0;
    }
}
