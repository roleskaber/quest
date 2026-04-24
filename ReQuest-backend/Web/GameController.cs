using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ReQuest_backend.Server.Auth;
using ReQuest_backend.Server;
using ReQuest_backend.Server.Database.Quest;
using ReQuest_backend.Server.QuestSession;
using ReQuest_backend.Web.DTO;

namespace ReQuest_backend.Web;

[Route("api/game")]
[ApiController]
public class GameController : ControllerBase
{
    private readonly IQuestService _questService;
    private readonly IQuestRepository _questRepository;
    private readonly IGameSessionStore _gameSessionStore;
    private readonly IAuthTokenService _authTokenService;

    public GameController(
        IQuestService questService,
        IQuestRepository questRepository,
        IGameSessionStore gameSessionStore,
        IAuthTokenService authTokenService
    )
    {
        _questService = questService;
        _questRepository = questRepository;
        _gameSessionStore = gameSessionStore;
        _authTokenService = authTokenService;
    }

    [HttpPost("create")]
    public async Task Create([FromBody] CreateGameRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUser(out var authUser))
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            await WriteSseEvent("error", new { message = "Сначала войдите или зарегистрируйтесь." }, cancellationToken);
            return;
        }

        Response.StatusCode = 200;
        Response.ContentType = "text/event-stream";

        var questionIds = new System.Collections.Generic.List<long>();

        await foreach (var generation in _questService.CreateNewQuestionsStream(
                           request.Count,
                           request.Difficulty,
                           request.Choice,
                           cancellationToken
                       ))
        {
            if (generation.Stage == "progress" && generation.QuestionId.HasValue)
            {
                questionIds.Add(generation.QuestionId.Value);
            }

            await WriteSseEvent("progress", new
            {
                stage = generation.Stage,
                created = generation.Created,
                total = generation.Total,
                message = generation.Message,
                questionText = generation.QuestionText,
                category = generation.Category
            }, cancellationToken);

            if (generation.Stage == "error")
            {
                await WriteSseEvent("error", new { message = generation.Message }, cancellationToken);
                return;
            }
        }

        if (questionIds.Count == 0)
        {
            
            await WriteSseEvent("error", new { message = "Не удалось сохранить вопросы для игры." }, cancellationToken);
            return;
        }

        var session = _gameSessionStore.Create(
            authUser!.Name,
            questionIds
        );

        await WriteSseEvent("completed", new GameLobbyResponse(
                session.Code,
                session.HostName,
                session.Players,
                session.QuestionIds.Count
            ),
            cancellationToken
        );
    }

    [HttpPost("join")]
    public ActionResult<GameLobbyResponse> Join([FromBody] JoinGameRequest request)
    {
        var session = _gameSessionStore.Join(request.Code.Trim(), request.PlayerName.Trim());
        if (session == null) return NotFound("Игра с таким кодом не найдена.");

        return Ok(new GameLobbyResponse(
            session.Code,
            session.HostName,
            session.Players,
            session.QuestionIds.Count
        ));
    }

    [HttpGet("lobby/{code}")]
    public ActionResult<GameLobbyResponse> GetLobby(string code)
    {
        var session = _gameSessionStore.Get(code.Trim());
        if (session == null) return NotFound("Игра с таким кодом не найдена.");

        return Ok(new GameLobbyResponse(
            session.Code,
            session.HostName,
            session.Players,
            session.QuestionIds.Count
        ));
    }

    [HttpGet("questions/{code}")]
    public async Task<ActionResult<List<QuestResponse>>> GetQuestions(string code)
    {
        var session = _gameSessionStore.Get(code.Trim());
        if (session == null) return NotFound("Игра с таким кодом не найдена.");

        var allQuests = await _questService.GetAllQuests();
        var byId = allQuests.ToDictionary(q => q.Id);
        var sessionQuests = session.QuestionIds
            .Where(byId.ContainsKey)
            .Select(id => byId[id])
            .ToList();

        var response = sessionQuests.Select(quest =>
        {
            var incorrectAnswers = JsonSerializer.Deserialize<List<string>>(quest.IncorrectAnswersJson) ?? [];
            return QuestResponse.FromEntity(quest, incorrectAnswers);
        }).ToList();

        return Ok(response);
    }

    [HttpGet("state/{code}")]
    public ActionResult<GameStateResponse> GetState(string code)
    {
        var state = _gameSessionStore.GetState(code.Trim());
        if (state == null) return NotFound("Игра с таким кодом не найдена.");

        return Ok(MapState(state));
    }

    [HttpPost("start")]
    public ActionResult<GameStateResponse> Start([FromBody] GameCodeRequest request)
    {
        var hostCheck = EnsureHost(request.Code, request.HostName);
        if (hostCheck != null) return hostCheck;

        var state = _gameSessionStore.Start(request.Code.Trim());
        if (state == null) return NotFound("Игра с таким кодом не найдена.");

        return Ok(MapState(state));
    }

    [HttpPost("next")]
    public ActionResult<GameStateResponse> NextQuestion([FromBody] GameCodeRequest request)
    {
        var hostCheck = EnsureHost(request.Code, request.HostName);
        if (hostCheck != null) return hostCheck;

        var state = _gameSessionStore.NextQuestion(request.Code.Trim());
        if (state == null) return NotFound("Игра с таким кодом не найдена.");

        return Ok(MapState(state));
    }

    [HttpPost("finish")]
    public ActionResult<GameStateResponse> Finish([FromBody] GameCodeRequest request)
    {
        var hostCheck = EnsureHost(request.Code, request.HostName);
        if (hostCheck != null) return hostCheck;

        var state = _gameSessionStore.Finish(request.Code.Trim());
        if (state == null) return NotFound("Игра с таким кодом не найдена.");

        return Ok(MapState(state));
    }

    [HttpPost("kick")]
    public ActionResult<GameStateResponse> KickPlayer([FromBody] KickPlayerRequest request)
    {
        var hostCheck = EnsureHost(request.Code, request.HostName);
        if (hostCheck != null) return hostCheck;

        var state = _gameSessionStore.KickPlayer(request.Code.Trim(), request.PlayerName.Trim());
        if (state == null) return NotFound("Игра с таким кодом не найдена.");

        return Ok(MapState(state));
    }

    [HttpPost("remove-question")]
    public ActionResult<GameStateResponse> RemoveQuestion([FromBody] RemoveQuestionRequest request)
    {
        var hostCheck = EnsureHost(request.Code, request.HostName);
        if (hostCheck != null) return hostCheck;

        var state = _gameSessionStore.RemoveQuestion(request.Code.Trim(), request.QuestionIndex);
        if (state == null) return NotFound("Игра с таким кодом не найдена.");

        return Ok(MapState(state));
    }

    [HttpPost("answer")]
    public async Task<ActionResult<GameStateResponse>> SubmitAnswer([FromBody] SubmitAnswerRequest request)
    {
        var session = _gameSessionStore.Get(request.Code.Trim());
        if (session == null) return NotFound("Игра с таким кодом не найдена.");

        if (session.CurrentQuestionIndex < 0 || session.CurrentQuestionIndex >= session.QuestionIds.Count)
        {
            return BadRequest("Нет активного вопроса.");
        }

        var questionId = session.QuestionIds[session.CurrentQuestionIndex];
        var question = await _questRepository.GetById(questionId);
        if (question == null) return NotFound("Вопрос не найден.");

        var isCorrect = string.Equals(
            request.Answer.Trim(),
            question.CorrectAnswer.Trim(),
            StringComparison.OrdinalIgnoreCase
        );

        var state = _gameSessionStore.SubmitAnswer(request.Code.Trim(), request.PlayerName.Trim(), isCorrect);
        if (state == null) return NotFound("Игра с таким кодом не найдена.");

        return Ok(MapState(state));
    }

    private static GameStateResponse MapState(GameSessionState state)
    {
        return new GameStateResponse(
            state.Code,
            state.HostName,
            state.IsStarted,
            state.IsFinished,
            state.CurrentQuestionIndex,
            state.QuestionsCount,
            state.QuestionStartedAt,
            state.QuestionTimeLimitSeconds,
            state.Players,
            state.Scores,
            state.AnsweredPlayers
        );
    }

    private ActionResult<GameStateResponse>? EnsureHost(string code, string hostName)
    {
        var session = _gameSessionStore.Get(code.Trim());
        if (session == null) return NotFound("Игра с таким кодом не найдена.");

        if (!string.Equals(session.HostName, hostName.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Только ведущий может выполнить это действие.");
        }

        return null;
    }

    private async Task WriteSseEvent(string eventName, object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload);
        await Response.WriteAsync($"event: {eventName}\n", cancellationToken);
        await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    private bool TryGetAuthenticatedUser(out AuthProfile? profile)
    {
        profile = null;

        if (!Request.Headers.TryGetValue("Authorization", out var headerValues)) return false;

        var headerValue = headerValues.ToString();
        const string bearerPrefix = "Bearer ";

        if (!headerValue.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)) return false;

        var token = headerValue[bearerPrefix.Length..].Trim();
        return _authTokenService.TryValidateToken(token, out profile);
    }
}
