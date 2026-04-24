using Microsoft.EntityFrameworkCore;
namespace ReQuest_backend.Server.Database.Quest;


public class QuestContext : DbContext
{
    public QuestContext(DbContextOptions<QuestContext> options) : base(options)
    {
    }

    public DbSet<QuestEntity> QuestEntities { get; set; } = null!;
    public DbSet<GameSessionEntity> GameSessions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QuestEntity>().ToTable("quests");
        modelBuilder.Entity<QuestEntity>().Property(q => q.IncorrectAnswersJson).HasColumnType("jsonb");

        modelBuilder.Entity<GameSessionEntity>().ToTable("game_sessions");
        modelBuilder.Entity<GameSessionEntity>().HasIndex(g => g.Code).IsUnique();
        modelBuilder.Entity<GameSessionEntity>().Property(g => g.Id).HasColumnName("id");
        modelBuilder.Entity<GameSessionEntity>().Property(g => g.Code).HasColumnName("code").HasMaxLength(6);
        modelBuilder.Entity<GameSessionEntity>().Property(g => g.HostName).HasColumnName("host_name").HasMaxLength(64);
        modelBuilder.Entity<GameSessionEntity>().Property(g => g.QuestionIdsJson).HasColumnName("question_ids_json").HasColumnType("jsonb");
        modelBuilder.Entity<GameSessionEntity>().Property(g => g.PlayersJson).HasColumnName("players_json").HasColumnType("jsonb");
        modelBuilder.Entity<GameSessionEntity>().Property(g => g.ScoresJson).HasColumnName("scores_json").HasColumnType("jsonb");
        modelBuilder.Entity<GameSessionEntity>().Property(g => g.AnsweredPlayersJson).HasColumnName("answered_players_json").HasColumnType("jsonb");
        modelBuilder.Entity<GameSessionEntity>().Property(g => g.CurrentQuestionIndex).HasColumnName("current_question_index");
        modelBuilder.Entity<GameSessionEntity>().Property(g => g.QuestionStartedAt).HasColumnName("question_started_at");
        modelBuilder.Entity<GameSessionEntity>().Property(g => g.QuestionTimeLimitSeconds).HasColumnName("question_time_limit_seconds");
        modelBuilder.Entity<GameSessionEntity>().Property(g => g.IsStarted).HasColumnName("is_started");
        modelBuilder.Entity<GameSessionEntity>().Property(g => g.IsFinished).HasColumnName("is_finished");
        modelBuilder.Entity<GameSessionEntity>().Property(g => g.CreatedAt).HasColumnName("created_at");
    }
}