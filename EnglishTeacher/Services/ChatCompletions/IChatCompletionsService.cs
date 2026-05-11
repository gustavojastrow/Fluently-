namespace EnglishTeacher.Services.ChatCompletions;

public interface IChatCompletionsService
{
    Task<string> CompleteAsync(string userMessage);

    Task<string> CompleteAsync(
        string systemPrompt,
        string userMessage,
        IEnumerable<(string Sender, string Message)>? history = null);

    IAsyncEnumerable<string> StreamAsync(
        string systemPrompt,
        string userMessage,
        IEnumerable<(string Sender, string Message)>? history = null,
        CancellationToken cancellationToken = default);
}
