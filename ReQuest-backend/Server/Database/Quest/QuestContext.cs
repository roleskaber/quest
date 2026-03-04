using Microsoft.EntityFrameworkCore;
namespace ReQuest_backend.Server.Database.Quest;

public class QuestContext : DbContext
{
    public DbSet<QuestEntity> QuestEntities { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql("Host=localhost;Port=5432;Database=questdb;Username=postgres;Password=1234");
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QuestEntity>().ToTable("QuestEntity");
    }
}