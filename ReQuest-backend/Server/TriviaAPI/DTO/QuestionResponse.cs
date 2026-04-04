using System.Collections.Generic;
using System.Text.Json.Serialization;
using ReQuest_backend.Server.TriviaAPI.DTO.Enums;

namespace ReQuest_backend.Server.TriviaAPI.DTO;

public record QuestionResponse(
    [property: JsonPropertyName("response_code")]
    int ResponseCode,
    [property: JsonPropertyName("results")]
    List<Question> Results
);

public record Question(
    [property: JsonPropertyName("type")]
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    QuestionChoiceType Type,
    [property: JsonPropertyName("difficulty")]
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    QuestionDifficultyType Difficulty,
    [property: JsonPropertyName("category")]
    string Category,
    [property: JsonPropertyName("question")]
    string QuestionText,
    [property: JsonPropertyName("correct_answer")]
    string CorrectAnswer,
    [property: JsonPropertyName("incorrect_answers")]
    List<string> IncorrectAnswers
);