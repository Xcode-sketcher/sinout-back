// --- REPOSITÓRIO DE PACIENTES: GERENCIAMENTO DE PACIENTES ---
// Responsável por todas as operações de banco de dados relacionadas aos pacientes

using MongoDB.Driver;
using APISinout.Models;

namespace APISinout.Data;

public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(int id);
    Task<List<Patient>> GetByCaregiverIdAsync(int caregiverId);
    Task<List<Patient>> GetAllAsync();
    Task CreatePatientAsync(Patient patient);
    Task UpdatePatientAsync(int id, Patient patient);
    Task DeletePatientAsync(int id);
    Task<int> GetNextPatientIdAsync();
    Task<bool> ExistsAsync(int id);
}

public class PatientRepository : IPatientRepository
{
    private readonly IMongoCollection<Patient> _patients;
    private readonly IMongoCollection<Counter> _counters;

    public PatientRepository(MongoDbContext context)
    {
        _patients = context.Patients;
        _counters = context.Counters;
    }

    public async Task<Patient?> GetByIdAsync(int id)
    {
        return await _patients.Find(p => p.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<Patient>> GetByCaregiverIdAsync(int caregiverId)
    {
        return await _patients.Find(p => p.CaregiverId == caregiverId && p.Status).ToListAsync();
    }

    public async Task<List<Patient>> GetAllAsync()
    {
        return await _patients.Find(p => p.Status).ToListAsync();
    }

    public async Task CreatePatientAsync(Patient patient)
    {
        await _patients.InsertOneAsync(patient);
    }

    public async Task UpdatePatientAsync(int id, Patient patient)
    {
        var filter = Builders<Patient>.Filter.Eq(p => p.Id, id);
        var update = Builders<Patient>.Update
            .Set(p => p.Name, patient.Name)
            .Set(p => p.CaregiverId, patient.CaregiverId)
            .Set(p => p.Status, patient.Status)
            .Set(p => p.AdditionalInfo, patient.AdditionalInfo)
            .Set(p => p.ProfilePhoto, patient.ProfilePhoto);
        
        await _patients.UpdateOneAsync(filter, update);
    }

    public async Task DeletePatientAsync(int id)
    {
        // Soft delete: apenas marca como inativo
        var filter = Builders<Patient>.Filter.Eq(p => p.Id, id);
        var update = Builders<Patient>.Update.Set(p => p.Status, false);
        await _patients.UpdateOneAsync(filter, update);
    }

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

    public async Task<bool> ExistsAsync(int id)
    {
        var count = await _patients.CountDocumentsAsync(p => p.Id == id);
        return count > 0;
    }
}
