
using EnglishTeacher.Models;

namespace EnglishTeacher.Services.Threads;

public interface IThreadService
{
    Task<ThreadResponse> CreateThreadAsync();
}