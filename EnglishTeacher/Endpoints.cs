using EnglishTeacher.Models;
using EnglishTeacher.Services.Assistants;
using EnglishTeacher.Services.Threads;

public static class Endpoints
{
    public static WebApplication UseAppEndpoints(this WebApplication app)
    {
        app.MapPost("/assistants", async (CreateTeacherRequest request, ITeacherService service) =>
                       Results.Ok(await service.CreateAssistantAsync(request)))
                   .WithName("CreateAssistant")
                   .WithOpenApi();

        app.MapPost("/assistants/basico", async (CreateTeacherRequest request, ITeacherService service) =>
                Results.Ok(await service.CreateTeacherBasicoAsync(request)))
            .WithName("CreateTeacherBasicoAsync")
            .WithOpenApi();

        app.MapPost("/teachers/intermediario", async (CreateTeacherRequest request, ITeacherService service) =>
                Results.Ok(await service.CreateTeacherIntermediarioAsync(request)))
            .WithName("CreateTeacherIntermediarioAsync")
            .WithOpenApi();

        app.MapPost("/teachers/avancado", async (CreateTeacherRequest request, ITeacherService service) =>
                Results.Ok(await service.CreateTeacherAvancadoAsync(request)))
            .WithName("CreateTeacherAvancadoAsync")
            .WithOpenApi();

        app.MapPost("/teachers/create/{type}", async (string type, CreateTeacherRequest request, ITeacherService service) =>
        {
            return type.ToLower() switch
            {
                "basico" => Results.Ok(await service.CreateTeacherBasicoAsync(request)),
                "intermediario" => Results.Ok(await service.CreateTeacherIntermediarioAsync(request)),
                "avancado" => Results.Ok(await service.CreateTeacherAvancadoAsync(request)),
                _ => Results.BadRequest($"Tipo de professor '{type}' não suportado. Use 'basico' ou 'intermediario' ou 'avancado'.")
            };
        })
        .WithName("CreateAssistantByType")
        .WithOpenApi();

        app.MapPost("/threads", async (IThreadService service) =>
                Results.Ok(await service.CreateThreadAsync()))
            .WithName("CreateThread")
            .WithOpenApi()
            .RequireAuthorization();


        app.MapPost("/run", async (RunRequest request, ITeacherService azureOpenApiService) =>
                Results.Ok(await azureOpenApiService.RunAsync(request)))
            .WithName("Run")
            .WithOpenApi()
            .RequireAuthorization();

        return app;
    }
}
