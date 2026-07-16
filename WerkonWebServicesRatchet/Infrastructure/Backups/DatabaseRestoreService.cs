using System.Diagnostics;
using Npgsql;

namespace WerkonWebServicesRatchet.Infrastructure.Backups;

public sealed class DatabaseRestoreService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseRestoreService> _logger;

    public DatabaseRestoreService(IConfiguration configuration, ILogger<DatabaseRestoreService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task RestoreAsync(string dumpFilePath, CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string is not configured.");

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var database = builder.Database
            ?? throw new InvalidOperationException("Database name is missing from connection string.");
        var host = builder.Host ?? "postgres";
        var port = builder.Port;
        var user = builder.Username ?? "postgres";
        var password = builder.Password ?? string.Empty;

        await RecreateDatabaseAsync(builder, database, cancellationToken);

        var psi = new ProcessStartInfo
        {
            FileName = "bash",
            ArgumentList =
            {
                "-lc",
                $"gunzip -c \"{dumpFilePath}\" | psql -v ON_ERROR_STOP=1 -h \"{host}\" -p {port} -U \"{user}\" -d \"{database}\""
            },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.Environment["PGPASSWORD"] = password;

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start psql restore process.");

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var stderr = await stderrTask;
        var stdout = await stdoutTask;

        if (process.ExitCode != 0)
        {
            _logger.LogError("Database restore failed. Exit={ExitCode}. Stderr={Stderr}. Stdout={Stdout}",
                process.ExitCode, stderr, stdout);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(stderr)
                    ? $"Restore failed with exit code {process.ExitCode}."
                    : stderr.Trim());
        }

        _logger.LogInformation("Database restore completed from {DumpFile}.", dumpFilePath);
    }

    private static async Task RecreateDatabaseAsync(
        NpgsqlConnectionStringBuilder source,
        string database,
        CancellationToken cancellationToken)
    {
        var admin = new NpgsqlConnectionStringBuilder(source.ConnectionString)
        {
            Database = "postgres"
        };

        await using var connection = new NpgsqlConnection(admin.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var terminate = connection.CreateCommand())
        {
            terminate.CommandText =
                """
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = @db AND pid <> pg_backend_pid();
                """;
            terminate.Parameters.AddWithValue("db", database);
            await terminate.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var drop = connection.CreateCommand())
        {
            drop.CommandText = $"DROP DATABASE IF EXISTS \"{EscapeIdentifier(database)}\" WITH (FORCE);";
            try
            {
                await drop.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (PostgresException)
            {
                drop.CommandText = $"DROP DATABASE IF EXISTS \"{EscapeIdentifier(database)}\";";
                await drop.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        await using (var create = connection.CreateCommand())
        {
            create.CommandText = $"CREATE DATABASE \"{EscapeIdentifier(database)}\";";
            await create.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static string EscapeIdentifier(string value) => value.Replace("\"", "\"\"", StringComparison.Ordinal);
}
