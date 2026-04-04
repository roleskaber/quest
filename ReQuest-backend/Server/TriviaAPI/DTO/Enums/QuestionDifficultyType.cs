using System.Runtime.Serialization;

namespace ReQuest_backend.Server.TriviaAPI.DTO.Enums;

public enum QuestionDifficultyType
{
    [EnumMember(Value = "easy")]
    Easy,
    [EnumMember(Value = "medium")]
    Medium,
    [EnumMember(Value = "hard")]
    Hard
}