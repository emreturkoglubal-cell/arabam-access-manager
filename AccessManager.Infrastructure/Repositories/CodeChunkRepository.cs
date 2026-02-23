using Npgsql;
using Pgvector;
using Pgvector.Npgsql;

namespace AccessManager.Infrastructure.Repositories;

public class CodeChunkRepository : ICodeChunkRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public CodeChunkRepository(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentNullException(nameof(connectionString));
        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.UseVector();
        _dataSource = builder.Build();
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("DELETE FROM code_chunks", conn);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task InsertBatchAsync(IReadOnlyList<(string RepoPath, string Content, float[] Embedding)> chunks, CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0) return;

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO code_chunks (repo_path, content, embedding) VALUES ($1, $2, $3)", conn);
        cmd.Parameters.Add(new NpgsqlParameter());
        cmd.Parameters.Add(new NpgsqlParameter());
        cmd.Parameters.Add(new NpgsqlParameter());

        foreach (var (repoPath, content, embedding) in chunks)
        {
            cmd.Parameters[0].Value = repoPath.Length > 500 ? repoPath[..500] : repoPath;
            cmd.Parameters[1].Value = content;
            cmd.Parameters[2].Value = new Vector(embedding);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<(string RepoPath, string Content)>> GetNearestAsync(float[] queryEmbedding, int limit, CancellationToken cancellationToken = default)
    {
        if (limit < 1) return Array.Empty<(string, string)>();
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(
            "SELECT repo_path, content FROM code_chunks ORDER BY embedding <=> $1 LIMIT $2", conn);
        cmd.Parameters.AddWithValue(new Vector(queryEmbedding));
        cmd.Parameters.AddWithValue(limit);

        var list = new List<(string, string)>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            list.Add((reader.GetString(0), reader.GetString(1)));
        return list;
    }

    public async Task<bool> HasAnyAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT 1 FROM code_chunks LIMIT 1", conn);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken);
    }
}
