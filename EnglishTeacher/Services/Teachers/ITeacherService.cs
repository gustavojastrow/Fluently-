using EnglishTeacher.Models;

namespace EnglishTeacher.Services.Assistants;

public interface ITeacherService
{
    Task<AssistantResponse> CreateAssistantAsync(CreateTeacherRequest request);
    Task<AssistantResponse> CreateTeacherBasicoAsync(CreateTeacherRequest request);
    Task<AssistantResponse> CreateTeacherIntermediarioAsync(CreateTeacherRequest request);
    Task<AssistantResponse> CreateTeacherAvancadoAsync(CreateTeacherRequest request);

    Task<RunResponse> RunAsync(RunRequest request, IEnumerable<(string Sender, string Message)>? history = null);

    // Retorna o system prompt enriquecido com memória de erros do aluno
    Task<string> GetEnrichedSystemPromptAsync(string login, string teacherId);

    IAsyncEnumerable<string> StreamAsync(
        string systemPrompt,
        string userMessage,
        IEnumerable<(string Sender, string Message)>? history = null,
        CancellationToken cancellationToken = default);
}
