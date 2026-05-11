using Microsoft.Data.SqlClient;

namespace EnglishTeacher.Services.Database;

public sealed class SqlProgressService(IConfiguration configuration, ILogger<SqlProgressService> logger)
{
    private string ConnectionString =>
        configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection não configurada.");

    // ── Vocabulary ────────────────────────────────────────────────────────────

    public async Task SaveVocabularyAsync(string login, string word, string translation, string level)
    {
        const string sql = """
            IF NOT EXISTS (
                SELECT 1 FROM dbo.UserVocabulary
                WHERE Login = @Login AND Word = @Word AND Level = @Level
            )
            INSERT INTO dbo.UserVocabulary (Login, Word, Translation, Level)
            VALUES (@Login, @Word, @Translation, @Level);
            """;
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Login", login);
            cmd.Parameters.AddWithValue("@Word", word);
            cmd.Parameters.AddWithValue("@Translation", translation);
            cmd.Parameters.AddWithValue("@Level", level);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao salvar vocabulário para {Login}.", login);
        }
    }

    public async Task<List<(string Word, string Translation)>> GetVocabularyAsync(string login, string level, int limit = 20)
    {
        const string sql = """
            SELECT TOP (@Limit) Word, Translation
            FROM dbo.UserVocabulary
            WHERE Login = @Login AND Level = @Level
            ORDER BY RegisteredAt DESC;
            """;
        var result = new List<(string, string)>();
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Login", login);
        cmd.Parameters.AddWithValue("@Level", level);
        cmd.Parameters.AddWithValue("@Limit", limit);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add((reader.GetString(0), reader.GetString(1)));
        return result;
    }

    // ── Errors ────────────────────────────────────────────────────────────────

    public async Task SaveErrorAsync(string login, string errorDescription, string correctedVersion, string level)
    {
        const string sql = """
            INSERT INTO dbo.UserErrors (Login, ErrorDescription, CorrectedVersion, Level)
            VALUES (@Login, @ErrorDescription, @CorrectedVersion, @Level);
            """;
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Login", login);
            cmd.Parameters.AddWithValue("@ErrorDescription", errorDescription);
            cmd.Parameters.AddWithValue("@CorrectedVersion", correctedVersion);
            cmd.Parameters.AddWithValue("@Level", level);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao salvar erro de {Login}.", login);
        }
    }

    public async Task<List<(string Error, string Correction)>> GetRecentErrorsAsync(string login, string level, int limit = 8)
    {
        const string sql = """
            SELECT TOP (@Limit) ErrorDescription, CorrectedVersion
            FROM dbo.UserErrors
            WHERE Login = @Login AND Level = @Level
            ORDER BY RegisteredAt DESC;
            """;
        var result = new List<(string, string)>();
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Login", login);
        cmd.Parameters.AddWithValue("@Level", level);
        cmd.Parameters.AddWithValue("@Limit", limit);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add((reader.GetString(0), reader.GetString(1)));
        return result;
    }

    // ── Topics ────────────────────────────────────────────────────────────────

    public async Task SaveTopicAsync(string login, string topic, string level)
    {
        const string sql = """
            IF NOT EXISTS (
                SELECT 1 FROM dbo.UserTopics
                WHERE Login = @Login AND Topic = @Topic AND Level = @Level
            )
            INSERT INTO dbo.UserTopics (Login, Topic, Level)
            VALUES (@Login, @Topic, @Level);
            """;
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Login", login);
            cmd.Parameters.AddWithValue("@Topic", topic);
            cmd.Parameters.AddWithValue("@Level", level);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao salvar tópico para {Login}.", login);
        }
    }

    // ── Exercises ─────────────────────────────────────────────────────────────

    public async Task<long> SaveExerciseAsync(string login, string question, string correctAnswer, string exerciseType, string level)
    {
        const string sql = """
            INSERT INTO dbo.UserExercises (Login, Question, CorrectAnswer, ExerciseType, Level)
            OUTPUT INSERTED.Id
            VALUES (@Login, @Question, @CorrectAnswer, @ExerciseType, @Level);
            """;
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Login", login);
        cmd.Parameters.AddWithValue("@Question", question);
        cmd.Parameters.AddWithValue("@CorrectAnswer", correctAnswer);
        cmd.Parameters.AddWithValue("@ExerciseType", exerciseType);
        cmd.Parameters.AddWithValue("@Level", level);
        var id = await cmd.ExecuteScalarAsync();
        return id is long l ? l : Convert.ToInt64(id);
    }

    public async Task<(string Question, string CorrectAnswer, string ExerciseType)?> GetPendingExerciseAsync(string login, string level)
    {
        const string sql = """
            SELECT TOP 1 Question, CorrectAnswer, ExerciseType
            FROM dbo.UserExercises
            WHERE Login = @Login AND Level = @Level AND WasAnswered = 0
            ORDER BY CreatedAt DESC;
            """;
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Login", login);
        cmd.Parameters.AddWithValue("@Level", level);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return (reader.GetString(0), reader.GetString(1), reader.GetString(2));
        return null;
    }

    public async Task MarkExerciseAnsweredAsync(string login, string level, bool wasCorrect)
    {
        const string sql = """
            UPDATE TOP (1) dbo.UserExercises
            SET WasAnswered = 1, WasCorrect = @WasCorrect
            WHERE Login = @Login AND Level = @Level AND WasAnswered = 0;
            """;
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Login", login);
            cmd.Parameters.AddWithValue("@Level", level);
            cmd.Parameters.AddWithValue("@WasCorrect", wasCorrect);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao marcar exercício de {Login}.", login);
        }
    }
}
