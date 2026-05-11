using EnglishTeacher.Models;

namespace EnglishTeacher.Services.Threads;

public partial class ThreadService
{
    public Task<ThreadResponse> CreateThreadAsync()
    {
        var thread = new ThreadResponse($"local-{Guid.NewGuid():N}");
        return Task.FromResult(thread);
    }
}
