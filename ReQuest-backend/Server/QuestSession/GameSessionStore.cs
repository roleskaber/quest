using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ReQuest_backend.Server.QuestSession;

public class GameSessionStore : IGameSessionStore
{
    private readonly ConcurrentDictionary<string, GameSession> _sessions = new();
    private readonly Random _random = new();

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
            Players = [hostName],
            Scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                [hostName] = 0
            },
            AnsweredPlayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            CurrentQuestionIndex = -1,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _sessions[code] = session;
        SessionCreated?.Invoke(this, new GameSessionCreatedEventArgs(session.Code, session.HostName, session.QuestionIds.Count));
        return session;
    }

    public GameSession? Join(string code, string playerName)
    {
        if (!_sessions.TryGetValue(code.ToUpperInvariant(), out var session)) return null;

        var added = false;

        lock (session)
        {
            var alreadyJoined = session.Players.Any(p => p.Equals(playerName, StringComparison.OrdinalIgnoreCase));
            if (!alreadyJoined)
            {
                session.Players.Add(playerName);
                if (!session.Scores.ContainsKey(playerName)) session.Scores[playerName] = 0;
                added = true;
            }
        }

        if (added)
        {
            PlayerJoined?.Invoke(this,
                new PlayerJoinedGameSessionEventArgs(session.Code, session.HostName, playerName, session.Players.Count));
        }

        return session;
    }

    public GameSession? Get(string code)
    {
        if (_sessions.TryGetValue(code.ToUpperInvariant(), out var session)) return session;
        return null;
    }

    public GameSessionState? GetState(string code)
    {
        if (!_sessions.TryGetValue(code.ToUpperInvariant(), out var session)) return null;

        lock (session)
        {
            return BuildState(session);
        }
    }

    public GameSessionState? Start(string code)
    {
        if (!_sessions.TryGetValue(code.ToUpperInvariant(), out var session)) return null;

        lock (session)
        {
            session.IsStarted = true;
            session.IsFinished = false;
            session.CurrentQuestionIndex = session.QuestionIds.Count > 0 ? 0 : -1;
            session.AnsweredPlayers.Clear();

            foreach (var player in session.Players)
            {
                session.Scores[player] = 0;
            }

            return BuildState(session);
        }
    }

    public GameSessionState? NextQuestion(string code)
    {
        if (!_sessions.TryGetValue(code.ToUpperInvariant(), out var session)) return null;

        lock (session)
        {
            if (!session.IsStarted || session.QuestionIds.Count == 0) return BuildState(session);

            if (session.CurrentQuestionIndex >= session.QuestionIds.Count - 1)
            {
                session.IsFinished = true;
                session.AnsweredPlayers.Clear();
                return BuildState(session);
            }

            session.CurrentQuestionIndex += 1;
            session.AnsweredPlayers.Clear();
            return BuildState(session);
        }
    }

    public GameSessionState? SubmitAnswer(string code, string playerName, bool isCorrect)
    {
        if (!_sessions.TryGetValue(code.ToUpperInvariant(), out var session)) return null;

        lock (session)
        {
            if (!session.IsStarted || session.IsFinished) return BuildState(session);
            if (session.CurrentQuestionIndex < 0 || session.CurrentQuestionIndex >= session.QuestionIds.Count) return BuildState(session);
            if (!session.Players.Any(p => p.Equals(playerName, StringComparison.OrdinalIgnoreCase))) return BuildState(session);

            var firstAnswer = session.AnsweredPlayers.Add(playerName);
            if (!firstAnswer) return BuildState(session);

            if (!session.Scores.ContainsKey(playerName)) session.Scores[playerName] = 0;
            if (isCorrect) session.Scores[playerName] += 1;

            return BuildState(session);
        }
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
            [.. session.Players],
            new Dictionary<string, int>(session.Scores, StringComparer.OrdinalIgnoreCase),
            [.. session.AnsweredPlayers]
        );
    }

    private string GenerateUniqueCode()
    {
        while (true)
        {
            var chars = new char[6];
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = (char)('0' + _random.Next(10));
            }

            var code = new string(chars);
            if (!_sessions.ContainsKey(code)) return code;
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
    List<string> Players,
    Dictionary<string, int> Scores,
    List<string> AnsweredPlayers
);
