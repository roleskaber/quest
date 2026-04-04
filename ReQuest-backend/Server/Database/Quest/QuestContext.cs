using Microsoft.EntityFrameworkCore;
namespace ReQuest_backend.Server.Database.Quest;


public class QuestContext : DbContext
{
    public QuestContext(DbContextOptions<QuestContext> options) : base(options)
    {
    }

    public DbSet<QuestEntity> QuestEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QuestEntity>().ToTable("quests");
        modelBuilder.Entity<QuestEntity>().Property(q => q.IncorrectAnswersJson).HasColumnType("jsonb");
    }
}