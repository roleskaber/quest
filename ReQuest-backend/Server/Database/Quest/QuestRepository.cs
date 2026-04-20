using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ReQuest_backend.Server.TriviaAPI.DTO;

namespace ReQuest_backend.Server.Database.Quest;

public class QuestRepository(QuestContext db) : IQuestRepository
{
    private QuestContext Db => db;

    public async Task<List<QuestEntity>> GetAll()
    {
        return await Db.QuestEntities
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<List<QuestEntity>> GetRandom(int count)
    {
        return await Db.QuestEntities
            .OrderBy(x => Guid.NewGuid())
            .Take(count)
            .ToListAsync();
    }

    public async Task<QuestEntity?> GetById(long id)
    {
        return await Db.QuestEntities.FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<QuestEntity?> Create(Question questionBody)
    {
        var entity = new QuestEntity
        {
            Category = questionBody.Category,
            QuestionText = questionBody.QuestionText,
            CorrectAnswer = questionBody.CorrectAnswer,
            IncorrectAnswersJson = JsonSerializer.Serialize(questionBody.IncorrectAnswers),
            Difficulty = questionBody.Difficulty,
            ChoiceType = questionBody.Type
        };

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
    
    public async Task<List<QuestEntity>> GetQuestionsByUserWhoAnswered(long id)
    {
        return await Db.QuestEntities
            .Where(q => q.UserAnswers.Any(e => e.UserId == id))
            .ToListAsync();
    }
}