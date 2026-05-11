using Microsoft.Data.SqlClient;

namespace EnglishTeacher.Services.Database;

public sealed class SqlChatHistoryService(IConfiguration configuration, ILogger<SqlChatHistoryService> logger)
{
    private string ConnectionString =>
        configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection nao configurada.");

    public async Task<bool> SaveMessageAsync(string userLogin, string sender, string message, string threadId, string assistantType)
    {
        const string sql = """
            INSERT INTO dbo.ChatMessages (Login, Remetente, Mensagem, ThreadId, TipoAssistente)
            VALUES (@Login, @Remetente, @Mensagem, @ThreadId, @TipoAssistente);
            """;

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Login", userLogin);
        command.Parameters.AddWithValue("@Remetente", sender);
        command.Parameters.AddWithValue("@Mensagem", message);
        command.Parameters.AddWithValue("@ThreadId", threadId);
        command.Parameters.AddWithValue("@TipoAssistente", assistantType);

        return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task<List<(string Sender, string Message)>> GetChatHistoryAsync(string userLogin, string assistantType)
    {
        const string sql = """
            SELECT Remetente, Mensagem
            FROM dbo.ChatMessages
            WHERE Login = @Login
              AND TipoAssistente = @TipoAssistente
              AND DataHoraRegistro >= DATEADD(DAY, -10, SYSUTCDATETIME())
            ORDER BY DataHoraRegistro, Id;
            """;

        var chatHistory = new List<(string Sender, string Message)>();

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Login", userLogin);
        command.Parameters.AddWithValue("@TipoAssistente", assistantType);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            chatHistory.Add((reader.GetString(0), reader.GetString(1)));
        }

        return chatHistory;
    }

    public async Task DeleteExpiredMessagesAsync()
    {
        const string sql = """
            DELETE FROM dbo.ChatMessages
            WHERE DataHoraRegistro < DATEADD(DAY, -10, SYSUTCDATETIME());
            """;

        try
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao remover mensagens expiradas do SQL Server.");
        }
    }
}
