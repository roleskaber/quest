
namespace ReQuest_backend.Server.Database.Quest;

public class GameSessionEntity
{
    public long Id { get; set; }
    public required string Code { get; set; }
    public required string HostName { get; set; }
    public required string QuestionIdsJson { get; set; }
    public required string PlayersJson { get; set; }
    public required string ScoresJson { get; set; }
    public required string AnsweredPlayersJson { get; set; }
    public int CurrentQuestionIndex { get; set; }
    public DateTimeOffset? QuestionStartedAt { get; set; }
    public int QuestionTimeLimitSeconds { get; set; } = 20;
    public bool IsStarted { get; set; }
    public bool IsFinished { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

