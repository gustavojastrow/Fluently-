using EnglishTeacher.Services.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Runtime.CompilerServices;

#pragma warning disable SKEXP0010

namespace EnglishTeacher.Services.ChatCompletions;

public class ChatCompletionsService(
    IChatCompletionService chatCompletionService,
    Kernel kernel,
    ProgressPlugin progressPlugin,
    ExercisePlugin exercisePlugin,
    IConfiguration configuration,
    ILogger<ChatCompletionsService> logger) : IChatCompletionsService
{
    // Kernel é transient no SK mas capturado aqui como scoped junto ao service.
    // Plugins são adicionados uma vez por request — sem sobrescrever o Kernel no DI.
    private bool _pluginsRegistered;

    private Kernel KernelWithPlugins()
    {
        if (!_pluginsRegistered)
        {
            kernel.Plugins.AddFromObject(progressPlugin, "Progress");
            kernel.Plugins.AddFromObject(exercisePlugin, "Exercise");
            _pluginsRegistered = true;
        }
        return kernel;
    }

    public Task<string> CompleteAsync(string userMessage)
        => CompleteAsync("You are a helpful English teacher.", userMessage);

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userMessage,
        IEnumerable<(string Sender, string Message)>? history = null)
    {
        var chatHistory = BuildChatHistory(systemPrompt, userMessage, history);

        // CompleteAsync usa AutoInvokeKernelFunctions — o SK chama ProgressPlugin/ExercisePlugin
        // automaticamente quando o modelo decide que é necessário (ex: salvar vocabulário).
        var settings = new OpenAIPromptExecutionSettings
        {
            Temperature = configuration.GetValue("Groq:Temperature", 0.4),
            MaxTokens = 1024,
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        try
        {
            var result = await chatCompletionService.GetChatMessageContentAsync(
                chatHistory, settings, KernelWithPlugins());

            return result.Content?.Trim() ?? string.Empty;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao chamar Groq via Semantic Kernel.");
            throw;
        }
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string systemPrompt,
        string userMessage,
        IEnumerable<(string Sender, string Message)>? history = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatHistory = BuildChatHistory(systemPrompt, userMessage, history);

        // Streaming sem ToolCallBehavior: function calling exige múltiplas viagens ao modelo
        // e bloqueia o stream até completar, causando hang de 60+ segundos.
        // Os plugins são salvos via CompleteAsync (endpoint não-streaming) quando necessário.
        var settings = new OpenAIPromptExecutionSettings
        {
            Temperature = configuration.GetValue("Groq:Temperature", 0.4),
            MaxTokens = 1024
        };

        await foreach (var chunk in chatCompletionService.GetStreamingChatMessageContentsAsync(
            chatHistory, settings, cancellationToken: cancellationToken))
        {
            var text = chunk.Content;
            if (!string.IsNullOrEmpty(text))
                yield return text;
        }
    }

    private static ChatHistory BuildChatHistory(
        string systemPrompt,
        string userMessage,
        IEnumerable<(string Sender, string Message)>? history)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        if (history is not null)
        {
            foreach (var (sender, message) in history.TakeLast(30))
            {
                if (sender.Equals("User", StringComparison.OrdinalIgnoreCase))
                    chatHistory.AddUserMessage(message);
                else
                    chatHistory.AddAssistantMessage(message);
            }
        }

        chatHistory.AddUserMessage(userMessage);
        return chatHistory;
    }
}

#pragma warning restore SKEXP0010
