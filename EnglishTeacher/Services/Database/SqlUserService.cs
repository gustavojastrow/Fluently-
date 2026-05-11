using Microsoft.Data.SqlClient;

namespace EnglishTeacher.Services.Database;

public sealed class SqlUserService(IConfiguration configuration)
{
    private string ConnectionString =>
        configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection nao configurada.");

    public async Task<UserAccount?> GetByLoginAsync(string login)
    {
        const string sql = """
            SELECT TOP 1 Id, Usuario, SenhaHash, Cargo, Tipo, Email, Nome
            FROM dbo.Users
            WHERE Usuario = @Usuario AND Ativo = 1;
            """;

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Usuario", login);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new UserAccount(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetInt32(4),
            reader.IsDBNull(5) ? null : reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetString(6));
    }

    public async Task<int> CreateAsync(string usuario, string senhaHash, string cargo, int tipo, string? email, string? nome)
    {
        const string sql = """
            INSERT INTO dbo.Users (Usuario, SenhaHash, Cargo, Tipo, Email, Nome)
            OUTPUT INSERTED.Id
            VALUES (@Usuario, @SenhaHash, @Cargo, @Tipo, @Email, @Nome);
            """;

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Usuario", usuario);
        command.Parameters.AddWithValue("@SenhaHash", senhaHash);
        command.Parameters.AddWithValue("@Cargo", cargo);
        command.Parameters.AddWithValue("@Tipo", tipo);
        command.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(email) ? DBNull.Value : email);
        command.Parameters.AddWithValue("@Nome", string.IsNullOrWhiteSpace(nome) ? DBNull.Value : nome);

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }
}
