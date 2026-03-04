using System.Diagnostics;
using Npgsql;

namespace EcommerceAPI.IntegrationTests;

internal static class IntegrationTestDatabaseManager
{
    private const string ContainerName = "ecommerce-integration-db";
    private const string DatabaseName = "ecommerce_test";
    private const string Username = "ecommerce_test_user";
    private const string Password = "test_password";
    private const string Image = "postgres:16-alpine";
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static string? _connectionString;

    public static async Task<string> GetConnectionStringAsync()
    {
        if (!string.IsNullOrWhiteSpace(_connectionString))
        {
            return _connectionString;
        }

        await Gate.WaitAsync();
        try
        {
            if (!string.IsNullOrWhiteSpace(_connectionString))
            {
                return _connectionString;
            }

            var configuredConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            if (!string.IsNullOrWhiteSpace(configuredConnectionString) &&
                await CanConnectAsync(configuredConnectionString))
            {
                _connectionString = configuredConnectionString;
                return _connectionString;
            }

            await EnsureDockerContainerAsync();

            var publishedPort = await EnsurePublishedPortAsync();
            _connectionString = BuildConnectionString(publishedPort);

            if (!await WaitUntilReachableAsync(_connectionString))
            {
                throw new InvalidOperationException(
                    $"Integration test database is not reachable after bootstrap. Connection string: {_connectionString}");
            }

            return _connectionString;
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task EnsureDockerContainerAsync()
    {
        if (!await DockerIsAvailableAsync())
        {
            throw new InvalidOperationException(
                "Docker is required for integration test database bootstrap when no working ConnectionStrings__DefaultConnection is provided.");
        }

        var containerState = await RunDockerAsync(
            "ps", "-a", "--filter", $"name=^{ContainerName}$", "--format", "{{.Status}}");

        if (string.IsNullOrWhiteSpace(containerState.StdOut))
        {
            await RunDockerAsync(
                "run", "-d",
                "--name", ContainerName,
                "-e", $"POSTGRES_DB={DatabaseName}",
                "-e", $"POSTGRES_USER={Username}",
                "-e", $"POSTGRES_PASSWORD={Password}",
                "-p", "5432",
                Image);
        }
        else if (containerState.StdOut.Contains("Exited", StringComparison.OrdinalIgnoreCase) ||
                 containerState.StdOut.Contains("Created", StringComparison.OrdinalIgnoreCase))
        {
            await RunDockerAsync("start", ContainerName);
        }
    }

    private static async Task<int> EnsurePublishedPortAsync()
    {
        var portOutput = await RunDockerAsync(false, "port", ContainerName, "5432/tcp");
        var port = ParsePublishedPort(portOutput.StdOut);
        if (port.HasValue)
        {
            return port.Value;
        }

        await RunDockerAsync(false, "rm", "-f", ContainerName);
        await RunDockerAsync(
            "run", "-d",
            "--name", ContainerName,
            "-e", $"POSTGRES_DB={DatabaseName}",
            "-e", $"POSTGRES_USER={Username}",
            "-e", $"POSTGRES_PASSWORD={Password}",
            "-p", "5432",
            Image);

        var recreatedPortOutput = await RunDockerAsync("port", ContainerName, "5432/tcp");
        var recreatedPort = ParsePublishedPort(recreatedPortOutput.StdOut);
        if (!recreatedPort.HasValue)
        {
            throw new InvalidOperationException(
                $"Unable to determine published port for Docker container '{ContainerName}'. Output: {recreatedPortOutput.StdOut} {recreatedPortOutput.StdErr}".Trim());
        }

        return recreatedPort.Value;
    }

    private static async Task<bool> DockerIsAvailableAsync()
    {
        var result = await RunProcessAsync("docker", "version --format '{{.Server.Version}}'", throwOnError: false);
        return result.ExitCode == 0;
    }

    private static async Task<bool> WaitUntilReachableAsync(string connectionString)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            if (await ContainerReportsReadyAsync() && await CanConnectAsync(connectionString))
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        return false;
    }

    private static async Task<bool> ContainerReportsReadyAsync()
    {
        var result = await RunDockerAsync(
            false,
            "exec", ContainerName, "pg_isready", "-U", Username, "-d", DatabaseName);

        return result.ExitCode == 0;
    }

    private static async Task<bool> CanConnectAsync(string connectionString)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await connection.CloseAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildConnectionString(int port)
        => $"Host=127.0.0.1;Port={port};Database={DatabaseName};Username={Username};Password={Password}";

    private static int? ParsePublishedPort(string? stdout)
    {
        if (string.IsNullOrWhiteSpace(stdout))
        {
            return null;
        }

        var firstLine = stdout
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(firstLine))
        {
            return null;
        }

        var lastColonIndex = firstLine.LastIndexOf(':');
        if (lastColonIndex < 0)
        {
            return null;
        }

        var portPart = firstLine[(lastColonIndex + 1)..].Trim();
        return int.TryParse(portPart, out var port) ? port : null;
    }

    private static Task<ProcessResult> RunDockerAsync(params string[] args)
        => RunDockerAsync(throwOnError: true, args);

    private static async Task<ProcessResult> RunDockerAsync(bool throwOnError, params string[] args)
    {
        var escapedArgs = string.Join(" ", args.Select(EscapeArgument));
        return await RunProcessAsync("docker", escapedArgs, throwOnError);
    }

    private static async Task<ProcessResult> RunProcessAsync(string fileName, string arguments, bool throwOnError)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var result = new ProcessResult(
            process.ExitCode,
            (await stdOutTask).Trim(),
            (await stdErrTask).Trim());

        if (throwOnError && result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Command failed: {fileName} {arguments}{Environment.NewLine}{result.StdErr}".Trim());
        }

        return result;
    }

    private static string EscapeArgument(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        return value.Any(char.IsWhiteSpace) || value.Contains('"')
            ? $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
            : value;
    }

    private sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);
}
