using System;
using Microsoft.EntityFrameworkCore;

namespace ReQuest_backend.Server.Database.User;

public class UserContext : DbContext
{
    public DbSet<UserEntity> UserEntities { get; set; } = null!;
    private readonly string? _databaseUri = Environment.GetEnvironmentVariable("DATABASE_URI");

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(_databaseUri);
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>().ToTable("QuestEntity");
    }
}