using EnglishTeacher.Models;

namespace EnglishTeacher.Services.Assistants;

public partial class TeacherService
{
    public async Task<RunResponse> RunAsync(
        RunRequest request,
        IEnumerable<(string Sender, string Message)>? history = null)
    {
        try
        {
            var teacherId = NormalizeTeacherId(request.AssistantId);
            var systemPrompt = GetSystemPrompt(teacherId);
            var responseMessage = await chatCompletionsService.CompleteAsync(systemPrompt, request.Message, history);

            if (string.IsNullOrWhiteSpace(responseMessage))
                responseMessage = "Não consegui gerar uma resposta agora. Pode reformular sua pergunta?";

            logger.LogInformation("Resposta gerada pelo professor {TeacherId} para thread {ThreadId}.", teacherId, request.ThreadId);
            return new RunResponse(responseMessage, 0m);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro durante a execução do professor local.");
            throw new Exception("Erro durante a execução do professor local: " + ex.Message, ex);
        }
    }
}
