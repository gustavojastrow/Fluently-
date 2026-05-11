using System.ComponentModel;
using EnglishTeacher.Services.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.SemanticKernel;

namespace EnglishTeacher.Services.Plugins;

// SK Plugin: gerencia exercícios gerados pelo professor.
// O modelo chama store_exercise ao criar um quiz/fill-in-the-blank e
// check_answer para validar a resposta do aluno contra a resposta correta armazenada.
public sealed class ExercisePlugin(
    SqlProgressService progressService,
    IHttpContextAccessor httpContextAccessor)
{
    private string Login => httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "unknown";

    [KernelFunction("store_exercise")]
    [Description("Store an exercise you just gave to the student so the answer can be validated later. Call this immediately after presenting any quiz, fill-in-the-blank or multiple-choice question.")]
    public async Task<string> StoreExercise(
        [Description("The full exercise question as shown to the student")] string question,
        [Description("The correct answer or expected response")] string correctAnswer,
        [Description("Type of exercise: 'fill-in-the-blank', 'multiple-choice', 'translation', 'sentence-correction', 'free-writing'")] string exerciseType,
        [Description("The student level: 'Nível Básico', 'Nível Intermediário' or 'Nível Avançado'")] string level)
    {
        var id = await progressService.SaveExerciseAsync(Login, question, correctAnswer, exerciseType, level);
        return $"Exercise #{id} stored successfully.";
    }

    [KernelFunction("check_answer")]
    [Description("Check if the student's answer to the pending exercise is correct. Returns whether it was correct and what the expected answer was. Call this when the student replies to an exercise you stored.")]
    public async Task<string> CheckAnswer(
        [Description("The student's answer as they wrote it")] string studentAnswer,
        [Description("The student level: 'Nível Básico', 'Nível Intermediário' or 'Nível Avançado'")] string level)
    {
        var exercise = await progressService.GetPendingExerciseAsync(Login, level);
        if (exercise is null)
            return "No pending exercise found for this student.";

        // Normaliza para comparação simples (case/space insensitive)
        var expected = exercise.Value.CorrectAnswer.Trim().ToLowerInvariant();
        var given = studentAnswer.Trim().ToLowerInvariant();
        var isCorrect = given.Contains(expected) || expected.Contains(given);

        await progressService.MarkExerciseAnsweredAsync(Login, level, isCorrect);

        return isCorrect
            ? $"CORRECT! The expected answer was: \"{exercise.Value.CorrectAnswer}\""
            : $"NOT CORRECT. The expected answer was: \"{exercise.Value.CorrectAnswer}\". Student answered: \"{studentAnswer}\"";
    }
}
