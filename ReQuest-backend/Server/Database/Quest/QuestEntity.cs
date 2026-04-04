using System.Collections.Generic;
using System;
using ReQuest_backend.Server.Database.UserAnswer;
using ReQuest_backend.Server.TriviaAPI.DTO.Enums;

namespace ReQuest_backend.Server.Database.Quest;

public class QuestEntity
{
    public long Id { get; set; }
    public required string Category { get; set; }
    public required string QuestionText { get; set; }
    public required string CorrectAnswer { get; set; }
    public required string IncorrectAnswersJson { get; set; }
    public QuestionDifficultyType Difficulty { get; set; }
    public QuestionChoiceType ChoiceType { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<UserAnswerEntity> UserAnswers { get; set; } = new List<UserAnswerEntity>();
}
