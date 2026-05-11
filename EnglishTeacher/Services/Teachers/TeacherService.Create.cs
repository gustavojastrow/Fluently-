using EnglishTeacher.Models;

namespace EnglishTeacher.Services.Assistants;

public partial class TeacherService
{
    public Task<AssistantResponse> CreateTeacherBasicoAsync(CreateTeacherRequest request)
        => CreateTeacherInternalAsync(request, "basico");

    public Task<AssistantResponse> CreateTeacherIntermediarioAsync(CreateTeacherRequest request)
        => CreateTeacherInternalAsync(request, "intermediario");

    public Task<AssistantResponse> CreateTeacherAvancadoAsync(CreateTeacherRequest request)
        => CreateTeacherInternalAsync(request, "avancado");

    public Task<AssistantResponse> CreateAssistantAsync(CreateTeacherRequest request)
        => CreateTeacherInternalAsync(request, "basico");

    private Task<AssistantResponse> CreateTeacherInternalAsync(CreateTeacherRequest request, string teacherId)
    {
        var name = string.IsNullOrWhiteSpace(request.Name) ? GetTeacherName(teacherId) : request.Name;
        var description = string.IsNullOrWhiteSpace(request.Description)
            ? GetSystemPrompt(teacherId)
            : request.Description;

        logger.LogInformation("Professor local {TeacherId} preparado: {Name}", teacherId, name);
        return Task.FromResult(new AssistantResponse(teacherId, name, description));
    }
}
