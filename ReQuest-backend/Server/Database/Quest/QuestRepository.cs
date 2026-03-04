using Microsoft.EntityFrameworkCore;

namespace ReQuest_backend.Server.Database.Quest;

public class QuestRepository(QuestContext db)
{
    private QuestContext Db => db;
    
    public async Task<List<QuestEntity>> GetAll()
    {
        return await Db.QuestEntities.ToListAsync();
    }
    
    public async Task<QuestEntity?> GetById(long id)
    {
        return await Db.QuestEntities.FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<QuestEntity?> Create(QuestEntity entity)
    {
        Db.QuestEntities.Add(entity);
        await Db.SaveChangesAsync();
        return entity;
    }

    public async Task<QuestEntity?> Update(QuestEntity entity)
    {
        Db.QuestEntities.Update(entity);
        await Db.SaveChangesAsync();
        return entity;
    }

    public async Task<List<QuestEntity>> GetQuestionsByUserWhoAnswered (long id)
    {
        return await Db.QuestEntities
            .Where(q => q.UserAnswers.Any(e => e.UserId == id))
            .ToListAsync();
    }
}