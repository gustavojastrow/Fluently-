using Microsoft.AspNetCore.Mvc;
using EnglishTeacher.Models;
using EnglishTeacher.Services.Assistants;
using EnglishTeacher.Services.Threads;
using Microsoft.AspNetCore.Authorization;
using EnglishTeacher.Services.Database;
using System.Text;
using System.Text.Json;

namespace EnglishTeacher.Controllers;

[Authorize]
public class TeacherController : Controller
{
    private readonly SqlChatHistoryService _chatHistoryService;

    public TeacherController(SqlChatHistoryService chatHistoryService)
    {
        _chatHistoryService = chatHistoryService;
    }

    [HttpGet("/Teacher")]
    public IActionResult Teacher() => View();

    [HttpGet("/BuscarTeacher")]
    public async Task<IActionResult> BuscarChat(
        [FromServices] IThreadService threadService,
        [FromQuery(Name = "assistantType")] string professorType = "Nível Básico")
    {
        try
        {
            professorType = NormalizeAssistantType(professorType);
            var userLogin = User.Identity!.Name;
            if (string.IsNullOrEmpty(userLogin))
                return Unauthorized(new { message = "Usuário não autenticado." });

            var threadId = await GetOrCreateThreadIdAsync(threadService, userLogin, professorType);
            var chatHistory = await _chatHistoryService.GetChatHistoryAsync(userLogin, professorType);

            if (chatHistory.Count == 0)
            {
                var welcome = GetWelcomeMessage(professorType);
                chatHistory.Add(("Assistant", welcome));
                await _chatHistoryService.SaveMessageAsync(userLogin, "Assistant", welcome, threadId, professorType);
            }

            return Json(new
            {
                success = true,
                chatHistory = chatHistory.Select(c => new { Sender = c.Sender, Message = c.Message }),
                assistantType = professorType
            });
        }
        catch
        {
            return Json(new { success = false, error = "Ocorreu um erro ao carregar o histórico." });
        }
    }

    // Endpoint de streaming — retorna text/event-stream com chunks do modelo em tempo real.
    // O cliente consome via fetch + ReadableStream sem esperar a resposta completa.
    [HttpPost("/ChatProfessorStream")]
    public async Task StreamChat(
        string message,
        string assistantType,
        [FromServices] ITeacherService teacherService,
        [FromServices] IThreadService threadService,
        [FromServices] ILogger<TeacherController> logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Response.StatusCode = 400;
            return;
        }

        var normalizedType = NormalizeAssistantType(assistantType);
        var userLogin = User.Identity?.Name;

        if (string.IsNullOrEmpty(userLogin))
        {
            Response.StatusCode = 401;
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        var threadId = await GetOrCreateThreadIdAsync(threadService, userLogin, normalizedType);

        // Carrega histórico do SQL para alimentar o contexto do modelo
        var history = await _chatHistoryService.GetChatHistoryAsync(userLogin, normalizedType);

        // Garante a mensagem de boas-vindas se for a primeira mensagem
        if (history.Count == 0)
        {
            var welcome = GetWelcomeMessage(normalizedType);
            history.Add(("Assistant", welcome));
            await _chatHistoryService.SaveMessageAsync(userLogin, "Assistant", welcome, threadId, normalizedType);
        }

        // Salva mensagem do usuário antes de chamar o modelo
        await _chatHistoryService.SaveMessageAsync(userLogin, "User", message, threadId, normalizedType);

        var fullResponse = new StringBuilder();

        try
        {
            // GetEnrichedSystemPromptAsync = prompt base + memória de erros do aluno (RAG leve)
            var systemPrompt = await teacherService.GetEnrichedSystemPromptAsync(userLogin, normalizedType);

            // Itera sobre os chunks do modelo conforme chegam do Groq
            await foreach (var chunk in teacherService.StreamAsync(systemPrompt, message, history, cancellationToken))
            {
                fullResponse.Append(chunk);

                // Formato SSE: "data: <payload>\n\n"
                var payload = JsonSerializer.Serialize(new { text = chunk });
                await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            // Salva a resposta completa no histórico após o streaming terminar
            var responseText = fullResponse.ToString();
            if (!string.IsNullOrWhiteSpace(responseText))
                await _chatHistoryService.SaveMessageAsync(userLogin, "Assistant", responseText, threadId, normalizedType);

            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Cliente desconectou — normal, não loga como erro
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro no streaming para {Login}.", userLogin);
            var errorPayload = JsonSerializer.Serialize(new { error = "Ocorreu um erro. Tente novamente." });
            await Response.WriteAsync($"data: {errorPayload}\n\n", cancellationToken);
        }

        await Response.Body.FlushAsync(cancellationToken);
    }

    // Mantido para compatibilidade com o fluxo antigo (sem streaming)
    [HttpPost("/ChatProfessor")]
    public async Task<IActionResult> Chat(
        IFormFile imageFile,
        string message,
        string assistantType,
        [FromServices] ILogger<TeacherController> logger,
        [FromServices] ITeacherService assistantService,
        [FromServices] IThreadService threadService)
    {
        try
        {
            var normalizedType = NormalizeAssistantType(assistantType);
            var userLogin = User.Identity?.Name;
            if (string.IsNullOrEmpty(userLogin))
                return Unauthorized(new { success = false, message = "Usuário não autenticado." });

            var threadId = await GetOrCreateThreadIdAsync(threadService, userLogin, normalizedType);
            var chatHistory = await _chatHistoryService.GetChatHistoryAsync(userLogin, normalizedType);

            if (!chatHistory.Any())
            {
                var welcome = GetWelcomeMessage(normalizedType);
                chatHistory.Add(("Assistant", welcome));
                await _chatHistoryService.SaveMessageAsync(userLogin, "Assistant", welcome, threadId, normalizedType);
            }

            if (!string.IsNullOrEmpty(message))
            {
                chatHistory.Add(("User", message));
                await _chatHistoryService.SaveMessageAsync(userLogin, "User", message, threadId, normalizedType);
            }

            var assistantResponses = new List<string>();

            if (!string.IsNullOrEmpty(message) && imageFile == null)
            {
                var runRequest = new RunRequest(normalizedType, threadId, message);
                // Passa o histórico completo para o modelo ter contexto da conversa
                var runResponse = await assistantService.RunAsync(runRequest, chatHistory);

                if (runResponse != null && !string.IsNullOrWhiteSpace(runResponse.Message))
                    assistantResponses.Add(runResponse.Message);
                else
                    assistantResponses.Add("O assistente não conseguiu fornecer uma solução.");
            }

            foreach (var response in assistantResponses)
            {
                chatHistory.Add(("Assistant", response));
                await _chatHistoryService.SaveMessageAsync(userLogin, "Assistant", response, threadId, normalizedType);
            }

            return Json(new
            {
                success = true,
                chatHistory = chatHistory.Select(c => new { sender = c.Item1, message = c.Item2 }),
                assistantType = normalizedType
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro no Chat Assistente");
            return Json(new { success = false, message = "Ocorreu um erro inesperado. Tente novamente mais tarde." });
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<string> GetOrCreateThreadIdAsync(IThreadService threadService, string userLogin, string assistantType)
    {
        var key = $"Thread_{userLogin}_{assistantType}";
        var threadId = HttpContext.Session.GetString(key);
        if (string.IsNullOrEmpty(threadId))
        {
            var thread = await threadService.CreateThreadAsync();
            threadId = thread.Id;
            HttpContext.Session.SetString(key, threadId);
        }
        return threadId;
    }

    private static string GetWelcomeMessage(string assistantType) => assistantType switch
    {
        "Nível Básico"        => "Olá! Eu sou a Luna, sua professora de inglês básico! 👋 Quer praticar vocabulário, frases do dia a dia ou tirar uma dúvida?",
        "Nível Intermediário" => "Hi! I'm Alex, your intermediate English teacher! 😊 Vamos praticar inglês com conversas, correções e exemplos naturais. What would you like to work on?",
        "Nível Avançado"      => "Hello! I'm Jordan, your advanced English coach. 🎯 I'm here to help you refine fluency, nuance, idioms, and precision. What shall we work on today?",
        _                     => "Olá! Como posso ajudar?"
    };

    private static string NormalizeAssistantType(string assistantType)
    {
        if (string.IsNullOrWhiteSpace(assistantType))
            return "Nível Básico";

        var n = assistantType.Trim().ToLowerInvariant()
            .Replace("í", "i").Replace("á", "a").Replace("â", "a")
            .Replace("ã", "a").Replace("é", "e").Replace("ê", "e")
            .Replace("ó", "o").Replace("ô", "o").Replace("ç", "c");

        if (n.Contains("avancado"))      return "Nível Avançado";
        if (n.Contains("intermediario")) return "Nível Intermediário";
        return "Nível Básico";
    }
}
