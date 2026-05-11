namespace EnglishTeacher.Services.Database;

public sealed record UserAccount(
    int Id,
    string Usuario,
    string SenhaHash,
    string Cargo,
    int Tipo,
    string? Email,
    string? Nome);
