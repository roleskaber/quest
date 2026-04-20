using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ReQuest_backend.Server;
using ReQuest_backend.Web.DTO;

namespace ReQuest_backend.Web;

[Route("api/quests")]
[ApiController]
public class QuestController : ControllerBase
{
    private readonly IQuestService _questService;

    public QuestController(IQuestService questService)
    {
        _questService = questService;
    }

    [HttpPost("create")]
    public async Task<ActionResult<List<QuestResponse>>> Create(
        [FromBody] CreateQuestionRequest dto
    )
    {
        var quests = await _questService.CreateNewQuestions(
            dto.Count, dto.Difficulty, dto.Choice
        );

        return Ok(MapToResponse(quests));
    }
    
    [HttpGet("all")]
    public async Task<ActionResult<List<QuestResponse>>> GetAll()
    {
        var quests = await _questService.GetAllQuests();
        return Ok(MapToResponse(quests));
    }

    private static List<QuestResponse> MapToResponse(List<Server.Database.Quest.QuestEntity> quests)
    {
        return quests.Select(quest =>
        {
            var incorrectAnswers = JsonSerializer.Deserialize<List<string>>(quest.IncorrectAnswersJson) ?? [];
            return QuestResponse.FromEntity(quest, incorrectAnswers);
        }).ToList();
    }
}