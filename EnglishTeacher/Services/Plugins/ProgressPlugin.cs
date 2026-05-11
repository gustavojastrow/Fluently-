using System.ComponentModel;
using EnglishTeacher.Services.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.SemanticKernel;

namespace EnglishTeacher.Services.Plugins;

// SK Plugin: funções que o modelo pode invocar automaticamente durante a conversa.
// O modelo decide quando chamar cada função baseado na descrição do [Description].
// IHttpContextAccessor fornece o login do usuário autenticado sem precisar passá-lo como parâmetro.
public sealed class ProgressPlugin(
    SqlProgressService progressService,
    IHttpContextAccessor httpContextAccessor)
{
    private string Login => httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "unknown";

    [KernelFunction("record_vocabulary")]
    [Description("Record a new English word or expression that the student just learned. Call this whenever you teach a new word, phrase or expression.")]
    public async Task RecordVocabulary(
        [Description("The English word or expression the student learned")] string word,
        [Description("The Portuguese translation or explanation")] string translation,
        [Description("The student level: 'Nível Básico', 'Nível Intermediário' or 'Nível Avançado'")] string level)
    {
        await progressService.SaveVocabularyAsync(Login, word, translation, level);
    }

    [KernelFunction("record_error")]
    [Description("Record a grammar or usage error the student made, along with the correct version. Call this whenever you correct a student mistake.")]
    public async Task RecordError(
        [Description("Brief description of the error pattern, e.g. 'Used since instead of for with duration'")] string errorDescription,
        [Description("The correct version or the corrected sentence")] string correctedVersion,
        [Description("The student level: 'Nível Básico', 'Nível Intermediário' or 'Nível Avançado'")] string level)
    {
        await progressService.SaveErrorAsync(Login, errorDescription, correctedVersion, level);
    }

    [KernelFunction("record_topic")]
    [Description("Record a topic that was fully covered in this lesson session. Call this when you finish teaching about a specific subject.")]
    public async Task RecordTopic(
        [Description("The topic that was covered, e.g. 'Greetings and introductions', 'Phrasal verbs with GET'")] string topic,
        [Description("The student level: 'Nível Básico', 'Nível Intermediário' or 'Nível Avançado'")] string level)
    {
        await progressService.SaveTopicAsync(Login, topic, level);
    }
}
