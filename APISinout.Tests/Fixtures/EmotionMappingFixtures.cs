using APISinout.Models;

namespace APISinout.Tests.Fixtures;

public static class EmotionMappingFixtures
{
    public static EmotionMapping CreateValidEmotionMapping(string? id = null, string? userId = null)
    {
        return new EmotionMapping
        {
            Id = id ?? MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            UserId = userId ?? MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Paciente está feliz!",
            Priority = 1,
            Active = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };
    }

    public static EmotionMappingRequest CreateValidEmotionMappingRequest(string? userId = null)
    {
        return new EmotionMappingRequest
        {
            UserId = userId ?? MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Paciente está feliz!",
            Priority = 1
        };
    }

    public static EmotionMappingResponse CreateValidEmotionMappingResponse(string? id = null, string? userId = null)
    {
        return new EmotionMappingResponse
        {
            Id = id ?? MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            UserId = userId ?? MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            UserName = "João Silva",
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Paciente está feliz!",
            Priority = 1,
            Active = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };
    }
}