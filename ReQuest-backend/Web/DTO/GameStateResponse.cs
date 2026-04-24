using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReQuest_backend.Web.DTO;

public record GameStateResponse(
    [property: JsonPropertyName("code")]
    string Code,
    [property: JsonPropertyName("hostName")]
    string HostName,
    [property: JsonPropertyName("isStarted")]
    bool IsStarted,
    [property: JsonPropertyName("isFinished")]
    bool IsFinished,
    [property: JsonPropertyName("currentQuestionIndex")]
    int CurrentQuestionIndex,
    [property: JsonPropertyName("questionsCount")]
    int QuestionsCount,
    [property: JsonPropertyName("questionStartedAt")]
    DateTimeOffset? QuestionStartedAt,
    [property: JsonPropertyName("questionTimeLimitSeconds")]
    int QuestionTimeLimitSeconds,
    [property: JsonPropertyName("players")]
    List<string> Players,
    [property: JsonPropertyName("scores")]
    Dictionary<string, int> Scores,
    [property: JsonPropertyName("answeredPlayers")]
    List<string> AnsweredPlayers
);