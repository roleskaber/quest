using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ReQuest_backend.Server.Database.Quest;

namespace ReQuest_backend.Server.QuestSession;

public class GameSessionStore : IGameSessionStore
{
    private readonly IDbContextFactory<QuestContext> _dbContextFactory;
    private readonly Random _random = new();
    private const int DefaultQuestionTimeLimitSeconds = 20;

    public GameSessionStore(IDbContextFactory<QuestContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public event GameSessionCreatedEventHandler? SessionCreated;
    public event PlayerJoinedGameSessionEventHandler? PlayerJoined;

    public GameSession Create(string hostName, List<long> questionIds)
    {
        var code = GenerateUniqueCode();

        var session = new GameSession
        {
            Code = code,
            HostName = hostName,
            QuestionIds = questionIds,
            Players = [],
            Scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
            AnsweredPlayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            CurrentQuestionIndex = -1,
            QuestionStartedAt = null,
            QuestionTimeLimitSeconds = DefaultQuestionTimeLimitSeconds,
            CreatedAt = DateTimeOffset.UtcNow
        };

        using var db = _dbContextFactory.CreateDbContext();
        var entity = new GameSessionEntity
        {
            Code = session.Code,
            HostName = session.HostName,
            QuestionIdsJson = JsonSerializer.Serialize(session.QuestionIds),
            PlayersJson = JsonSerializer.Serialize(session.Players),
            ScoresJson = JsonSerializer.Serialize(session.Scores),
            AnsweredPlayersJson = JsonSerializer.Serialize(session.AnsweredPlayers),
            CurrentQuestionIndex = session.CurrentQuestionIndex,
            QuestionStartedAt = session.QuestionStartedAt,
            QuestionTimeLimitSeconds = session.QuestionTimeLimitSeconds,
            IsStarted = session.IsStarted,
            IsFinished = session.IsFinished,
            CreatedAt = session.CreatedAt
        };

        db.GameSessions.Add(entity);
        db.SaveChanges();

        SessionCreated?.Invoke(this, new GameSessionCreatedEventArgs(session.Code, session.HostName, session.QuestionIds.Count));
        return session;
    }

    public GameSession? Join(string code, string playerName)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var session = LoadSession(db, code);
        if (session == null) return null;

        var added = false;

        if (!playerName.Equals(session.HostName, StringComparison.OrdinalIgnoreCase))
        {
            var alreadyJoined = session.Players.Any(p => p.Equals(playerName, StringComparison.OrdinalIgnoreCase));
            if (!alreadyJoined)
            {
                session.Players.Add(playerName);
                if (!session.Scores.ContainsKey(playerName)) session.Scores[playerName] = 0;
                added = true;
            }
        }

        if (added) SaveSession(db, session);

        if (added)
        {
            PlayerJoined?.Invoke(this,
                new PlayerJoinedGameSessionEventArgs(session.Code, session.HostName, playerName, session.Players.Count));
        }

        return session;
    }

    public GameSession? Get(string code)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return LoadSession(db, code);
    }

    public GameSessionState? GetState(string code)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var session = LoadSession(db, code);
        return session == null ? null : BuildState(session);
    }

    public GameSessionState? Start(string code)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var session = LoadSession(db, code);
        if (session == null) return null;

        session.IsStarted = true;
        session.IsFinished = false;
        session.CurrentQuestionIndex = session.QuestionIds.Count > 0 ? 0 : -1;
        session.QuestionStartedAt = session.CurrentQuestionIndex >= 0 ? DateTimeOffset.UtcNow : null;
        session.QuestionTimeLimitSeconds = DefaultQuestionTimeLimitSeconds;
        session.AnsweredPlayers.Clear();

        foreach (var player in session.Players)
        {
            session.Scores[player] = 0;
        }

        SaveSession(db, session);
        return BuildState(session);
    }

    public GameSessionState? NextQuestion(string code)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var session = LoadSession(db, code);
        if (session == null) return null;

        if (!session.IsStarted || session.QuestionIds.Count == 0) return BuildState(session);

        if (session.CurrentQuestionIndex >= session.QuestionIds.Count - 1)
        {
            session.IsFinished = true;
            session.QuestionStartedAt = null;
            session.AnsweredPlayers.Clear();
            SaveSession(db, session);
            return BuildState(session);
        }

        session.CurrentQuestionIndex += 1;
        session.QuestionStartedAt = DateTimeOffset.UtcNow;
        session.QuestionTimeLimitSeconds = DefaultQuestionTimeLimitSeconds;
        session.AnsweredPlayers.Clear();
        SaveSession(db, session);
        return BuildState(session);
    }

    public GameSessionState? Finish(string code)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var session = LoadSession(db, code);
        if (session == null) return null;

        session.IsFinished = true;
        session.QuestionStartedAt = null;
        session.AnsweredPlayers.Clear();
        SaveSession(db, session);
        return BuildState(session);
    }

    public GameSessionState? KickPlayer(string code, string playerName)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var session = LoadSession(db, code);
        if (session == null) return null;

        if (playerName.Equals(session.HostName, StringComparison.OrdinalIgnoreCase)) return BuildState(session);

        var existingPlayer = session.Players.FirstOrDefault(p => p.Equals(playerName, StringComparison.OrdinalIgnoreCase));
        if (existingPlayer == null) return BuildState(session);

        session.Players.Remove(existingPlayer);
        session.Scores.Remove(existingPlayer);
        session.AnsweredPlayers.Remove(existingPlayer);
        SaveSession(db, session);
        return BuildState(session);
    }

    public GameSessionState? RemoveQuestion(string code, int questionIndex)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var session = LoadSession(db, code);
        if (session == null) return null;

        if (questionIndex < 0 || questionIndex >= session.QuestionIds.Count) return BuildState(session);
        if (session.QuestionIds.Count <= 1) return BuildState(session);

        session.QuestionIds.RemoveAt(questionIndex);

        if (session.CurrentQuestionIndex > questionIndex)
        {
            session.CurrentQuestionIndex -= 1;
        }

        if (session.CurrentQuestionIndex >= session.QuestionIds.Count)
        {
            session.CurrentQuestionIndex = session.QuestionIds.Count - 1;
        }

        if (session.QuestionIds.Count == 0)
        {
            session.CurrentQuestionIndex = -1;
            session.IsFinished = true;
        }

        session.AnsweredPlayers.Clear();
        SaveSession(db, session);
        return BuildState(session);
    }

    public GameSessionState? SubmitAnswer(string code, string playerName, bool isCorrect)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var session = LoadSession(db, code);
        if (session == null) return null;

        if (!session.IsStarted || session.IsFinished) return BuildState(session);
        if (session.CurrentQuestionIndex < 0 || session.CurrentQuestionIndex >= session.QuestionIds.Count) return BuildState(session);
        if (!session.Players.Any(p => p.Equals(playerName, StringComparison.OrdinalIgnoreCase))) return BuildState(session);
        if (IsQuestionExpired(session, DateTimeOffset.UtcNow)) return BuildState(session);

        var firstAnswer = session.AnsweredPlayers.Add(playerName);
        if (!firstAnswer) return BuildState(session);

        if (!session.Scores.ContainsKey(playerName)) session.Scores[playerName] = 0;
        if (isCorrect) session.Scores[playerName] += 1;

        SaveSession(db, session);
        return BuildState(session);
    }

    private GameSession? LoadSession(QuestContext db, string code)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var entity = db.GameSessions.AsNoTracking().FirstOrDefault(s => s.Code == normalizedCode);
        return entity == null ? null : ToDomain(entity);
    }

    private static void SaveSession(QuestContext db, GameSession session)
    {
        var normalizedCode = session.Code.Trim().ToUpperInvariant();
        var entity = db.GameSessions.FirstOrDefault(s => s.Code == normalizedCode);
        if (entity == null) return;

        entity.HostName = session.HostName;
        entity.QuestionIdsJson = JsonSerializer.Serialize(session.QuestionIds);
        entity.PlayersJson = JsonSerializer.Serialize(session.Players);
        entity.ScoresJson = JsonSerializer.Serialize(session.Scores);
        entity.AnsweredPlayersJson = JsonSerializer.Serialize(session.AnsweredPlayers);
        entity.CurrentQuestionIndex = session.CurrentQuestionIndex;
        entity.QuestionStartedAt = session.QuestionStartedAt;
        entity.QuestionTimeLimitSeconds = session.QuestionTimeLimitSeconds;
        entity.IsStarted = session.IsStarted;
        entity.IsFinished = session.IsFinished;

        db.SaveChanges();
    }

    private static GameSession ToDomain(GameSessionEntity entity)
    {
        var questionIds = JsonSerializer.Deserialize<List<long>>(entity.QuestionIdsJson) ?? [];
        var players = JsonSerializer.Deserialize<List<string>>(entity.PlayersJson) ?? [];
        var scores = JsonSerializer.Deserialize<Dictionary<string, int>>(entity.ScoresJson)
                     ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var answeredPlayers = JsonSerializer.Deserialize<HashSet<string>>(entity.AnsweredPlayersJson)
                              ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return new GameSession
        {
            Code = entity.Code,
            HostName = entity.HostName,
            QuestionIds = questionIds,
            Players = players,
            Scores = new Dictionary<string, int>(scores, StringComparer.OrdinalIgnoreCase),
            AnsweredPlayers = new HashSet<string>(answeredPlayers, StringComparer.OrdinalIgnoreCase),
            CurrentQuestionIndex = entity.CurrentQuestionIndex,
            QuestionStartedAt = entity.QuestionStartedAt,
            QuestionTimeLimitSeconds = entity.QuestionTimeLimitSeconds > 0 ? entity.QuestionTimeLimitSeconds : DefaultQuestionTimeLimitSeconds,
            IsStarted = entity.IsStarted,
            IsFinished = entity.IsFinished,
            CreatedAt = entity.CreatedAt
        };
    }

    private static GameSessionState BuildState(GameSession session)
    {
        return new GameSessionState(
            session.Code,
            session.HostName,
            session.IsStarted,
            session.IsFinished,
            session.CurrentQuestionIndex,
            session.QuestionIds.Count,
            session.QuestionStartedAt,
            session.QuestionTimeLimitSeconds,
            [.. session.Players],
            new Dictionary<string, int>(session.Scores, StringComparer.OrdinalIgnoreCase),
            [.. session.AnsweredPlayers]
        );
    }

    private static bool IsQuestionExpired(GameSession session, DateTimeOffset now)
    {
        if (!session.QuestionStartedAt.HasValue || session.QuestionTimeLimitSeconds <= 0) return false;
        return now >= session.QuestionStartedAt.Value.AddSeconds(session.QuestionTimeLimitSeconds);
    }

    private string GenerateUniqueCode()
    {
        using var db = _dbContextFactory.CreateDbContext();

        while (true)
        {
            var chars = new char[6];
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = (char)('0' + _random.Next(10));
            }

            var code = new string(chars);
            var exists = db.GameSessions.AsNoTracking().Any(s => s.Code == code);
            if (!exists) return code;
        }
    }
}

public delegate void GameSessionCreatedEventHandler(object? sender, GameSessionCreatedEventArgs args);

public delegate void PlayerJoinedGameSessionEventHandler(object? sender, PlayerJoinedGameSessionEventArgs args);

public class GameSessionCreatedEventArgs : EventArgs
{
    public GameSessionCreatedEventArgs(string code, string hostName, int questionCount)
    {
        Code = code;
        HostName = hostName;
        QuestionCount = questionCount;
    }

    public string Code { get; }
    public string HostName { get; }
    public int QuestionCount { get; }
}

public class PlayerJoinedGameSessionEventArgs : EventArgs
{
    public PlayerJoinedGameSessionEventArgs(string code, string hostName, string playerName, int playersCount)
    {
        Code = code;
        HostName = hostName;
        PlayerName = playerName;
        PlayersCount = playersCount;
    }

    public string Code { get; }
    public string HostName { get; }
    public string PlayerName { get; }
    public int PlayersCount { get; }
}

public class GameSession
{
    public required string Code { get; init; }
    public required string HostName { get; init; }
    public required List<string> Players { get; init; }
    public required List<long> QuestionIds { get; init; }
    public required Dictionary<string, int> Scores { get; init; }
    public required HashSet<string> AnsweredPlayers { get; init; }
    public required int CurrentQuestionIndex { get; set; }
    public DateTimeOffset? QuestionStartedAt { get; set; }
    public int QuestionTimeLimitSeconds { get; set; }
    public bool IsStarted { get; set; }
    public bool IsFinished { get; set; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public record GameSessionState(
    string Code,
    string HostName,
    bool IsStarted,
    bool IsFinished,
    int CurrentQuestionIndex,
    int QuestionsCount,
    DateTimeOffset? QuestionStartedAt,
    int QuestionTimeLimitSeconds,
    List<string> Players,
    Dictionary<string, int> Scores,
    List<string> AnsweredPlayers
);
