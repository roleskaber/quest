using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ReQuest_backend.Server.Database.Quest;
using ReQuest_backend.Server.DTO;
using ReQuest_backend.Server.Database.User;
using ReQuest_backend.Server.QuestSession.DTO;
using ReQuest_backend.Server.QuestSession.Redis;
using ReQuest_backend.Server.Translation;
using ReQuest_backend.Server.TriviaAPI;
using ReQuest_backend.Server.TriviaAPI.DTO.Enums;

namespace ReQuest_backend.Server;

public class QuestService
{
    private readonly TriviaApiService _triviaApiService;
    private readonly QuestRepository _questRepository;
    private readonly QuestionTranslationService _questionTranslationService;

    public QuestService(
        TriviaApiService triviaApiService,
        QuestRepository questRepository,
        QuestionTranslationService questionTranslationService
    )
    {
        _triviaApiService = triviaApiService;
        _questRepository = questRepository;
        _questionTranslationService = questionTranslationService;
    }

    public async Task<List<QuestEntity>> CreateNewQuestions(
        int count,
        QuestionDifficultyType? difficulty,
        QuestionChoiceType? choiceType
    )
    {
        var token = await _triviaApiService.GetToken();
        if (token == null) return [];

        var questions = await _triviaApiService.GetQuestions(count, token, difficulty, choiceType);
        List<QuestEntity> questEntities = [];
        foreach (var questionResponse in questions)
        {
            var translatedQuestion = await _questionTranslationService.TranslateToRussian(questionResponse);
            var entity = await _questRepository.Create(translatedQuestion);
            if (entity != null) questEntities.Add(entity);
        }

        return questEntities;
    }

    public async IAsyncEnumerable<QuestionGenerationProgress> CreateNewQuestionsStream(
        int count,
        QuestionDifficultyType? difficulty,
        QuestionChoiceType? choiceType,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        yield return new QuestionGenerationProgress("init", 0, 0, "Получаю токен вопросов...");

        var token = await _triviaApiService.GetToken();
        if (token == null)
        {
            yield return new QuestionGenerationProgress("error", 0, 0, "Не удалось получить токен OpenTDB.");
            yield break;
        }

        yield return new QuestionGenerationProgress("init", 0, 0, "Загружаю вопросы...");

        var questions = await _triviaApiService.GetQuestions(count, token, difficulty, choiceType);
        if (questions.Count == 0)
        {
            yield return new QuestionGenerationProgress("error", 0, 0, "API не вернул вопросов по заданным параметрам.");
            yield break;
        }

        yield return new QuestionGenerationProgress("fetched", 0, questions.Count, "Вопросы получены, начинаю генерацию...");

        var created = 0;
        var total = questions.Count;

        foreach (var questionResponse in questions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var translatedQuestion = await _questionTranslationService.TranslateToRussian(questionResponse);
            var entity = await _questRepository.Create(translatedQuestion);
            if (entity == null) continue;

            created += 1;
            yield return new QuestionGenerationProgress(
                "progress",
                created,
                total,
                $"Создан вопрос {created} из {total}",
                entity.Id,
                translatedQuestion.QuestionText,
                translatedQuestion.Category
            );
        }

        yield return new QuestionGenerationProgress("done", created, total, "Генерация завершена.");
    }

    public async Task<List<QuestEntity>> GetAllQuests() => await _questRepository.GetAll();

    public async Task<StartSessionResponse> StartQuestSession(UserEntity user, int count)
    {
        var questEntities = await  _questRepository.GetRandom(count);
        var token = await new SessionCache().CreateSessionToken(user);
        return new StartSessionResponse(
            token, questEntities
        );

    }
}