using System.ComponentModel.DataAnnotations;
using ReQuest.Controllers.Database.UserAnswer;

namespace ReQuest_backend.Server.Database.Quest;

public class QuestEntity
{
    public long Id { get; set; }
    public required string Type { get; set; }
    public required string Difficulty { get; set; }
    public required string Category { get; set; }
    public required string Text { get; set; }
    public required string CorrectAnswer { get; set; }
    public required List<UserAnswerEntity> UserAnswers { get; set; }
}