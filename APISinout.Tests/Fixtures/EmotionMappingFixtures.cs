using APISinout.Models;

namespace APISinout.Tests.Fixtures;

public static class EmotionMappingFixtures
{
    public static EmotionMapping CreateValidEmotionMapping(string? id = null, int userId = 1)
    {
        return new EmotionMapping
        {
            Id = id ?? MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            UserId = userId,
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

    public static EmotionMappingRequest CreateValidEmotionMappingRequest(int userId = 1)
    {
        return new EmotionMappingRequest
        {
            UserId = userId,
            Emotion = "happy",
            IntensityLevel = "high",
            MinPercentage = 80.0,
            Message = "Paciente está feliz!",
            Priority = 1
        };
    }

    public static EmotionMappingResponse CreateValidEmotionMappingResponse(string? id = null, int userId = 1)
    {
        return new EmotionMappingResponse
        {
            Id = id ?? MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            UserId = userId,
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