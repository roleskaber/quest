using ReQuest_backend.Server.Database.Quest;

namespace ReQuest_backend.Server.Database.User;

public class UserEntity
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required List<QuestEntity> AnsweredQuests { get; set; }
    public required List<QuestEntity> CreatedQuests { get; set; }
}