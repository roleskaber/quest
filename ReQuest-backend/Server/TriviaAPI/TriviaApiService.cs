using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using ReQuest_backend.Server.TriviaAPI.DTO;
using ReQuest_backend.Server.TriviaAPI.DTO.Enums;

namespace ReQuest_backend.Server.TriviaAPI;

public class TriviaApiService
{
    private readonly HttpClient _httpClient;

    public TriviaApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string?> GetToken()
    {
        var response = await _httpClient.GetFromJsonAsync<TokenResponse>(
            "https://opentdb.com/api_token.php?command=request"
        );

        return response?.Token;
    }

    public async Task<List<Question>> GetQuestions(
        int count,
        string token,
        QuestionDifficultyType? difficulty,
        QuestionChoiceType? choiceType
    )
    {
        var query = new Dictionary<string, string?>
        {
            ["amount"] = count.ToString(),
            ["difficulty"] = difficulty?.ToString().ToLowerInvariant(),
            ["type"] = choiceType?.ToString().ToLowerInvariant(),
            ["token"] = token
        };

        var url = QueryHelpers.AddQueryString("https://opentdb.com/api.php", query);
        var response = await _httpClient.GetFromJsonAsync<QuestionResponse>(url);

        if (response == null || response.ResponseCode != 0) return [];

        return response.Results
            .Select(question => question with
            {
                Category = WebUtility.HtmlDecode(question.Category),
                QuestionText = WebUtility.HtmlDecode(question.QuestionText),
                CorrectAnswer = WebUtility.HtmlDecode(question.CorrectAnswer),
                IncorrectAnswers = question.IncorrectAnswers
                    .Select(WebUtility.HtmlDecode)
                    .ToList()
            })
            .ToList();
    }
}