using System.Diagnostics;
using System.Text;

namespace ForgeDotNet.CodeRunner;

internal sealed class DockerCli(DockerCodeRunnerOptions options)
{
    private const int MaximumCapturedCharacters = 512 * 1024;

    public async Task<DockerCommandResult> RunAsync(
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default) =>
        await RunCoreAsync(arguments, timeout, null, cancellationToken);

    public async Task<DockerCommandResult> RunAsync(
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        string standardInput,
        CancellationToken cancellationToken = default) =>
        await RunCoreAsync(arguments, timeout, standardInput, cancellationToken);

    private async Task<DockerCommandResult> RunCoreAsync(
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        string? standardInput,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        cancellationToken.ThrowIfCancellationRequested();
        if (arguments.Count == 0)
        {
            throw new ArgumentException("Une commande Docker contrôlée est obligatoire.", nameof(arguments));
        }

        var startInfo = new ProcessStartInfo(options.DockerExecutablePath)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = standardInput is not null,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("--context");
        startInfo.ArgumentList.Add(options.DockerContext);
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        DateTimeOffset startedAtUtc = DateTimeOffset.UtcNow;
        try
        {
            if (!process.Start())
            {
                return DockerCommandResult.NotStarted(startedAtUtc);
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return DockerCommandResult.NotStarted(startedAtUtc);
        }

        Task<CapturedText> standardOutput = ReadBoundedAsync(process.StandardOutput);
        Task<CapturedText> standardError = ReadBoundedAsync(process.StandardError);
        Task input = standardInput is null
            ? Task.CompletedTask
            : WriteInputAsync(process.StandardInput, standardInput);
        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);
        bool cancelled = false;
        bool timedOut = false;
        try
        {
            await process.WaitForExitAsync(linkedSource.Token);
        }
        catch (OperationCanceledException)
        {
            cancelled = cancellationToken.IsCancellationRequested;
            timedOut = !cancelled && timeoutSource.IsCancellationRequested;
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            await process.WaitForExitAsync(CancellationToken.None);
        }

        CapturedText stdout = await standardOutput;
        CapturedText stderr = await standardError;
        await input;
        if (cancelled || cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        return new DockerCommandResult(
            cancelled || timedOut ? -1 : process.ExitCode,
            stdout.Text,
            stderr.Text,
            stdout.Truncated || stderr.Truncated,
            timedOut,
            cancelled,
            Started: true,
            startedAtUtc,
            DateTimeOffset.UtcNow);
    }

    private static async Task WriteInputAsync(StreamWriter writer, string value)
    {
        try
        {
            await writer.WriteLineAsync(value);
            await writer.FlushAsync();
        }
        catch (IOException)
        {
        }
        finally
        {
            writer.Close();
        }
    }

    private static async Task<CapturedText> ReadBoundedAsync(StreamReader reader)
    {
        char[] buffer = new char[4_096];
        var output = new StringBuilder();
        bool truncated = false;
        while (true)
        {
            int read = await reader.ReadAsync(buffer.AsMemory(), CancellationToken.None);
            if (read == 0)
            {
                break;
            }

            if (output.Length < MaximumCapturedCharacters)
            {
                int accepted = Math.Min(read, MaximumCapturedCharacters - output.Length);
                output.Append(buffer, 0, accepted);
                truncated |= accepted < read;
            }
            else
            {
                truncated = true;
            }
        }

        return new CapturedText(output.ToString(), truncated);
    }

    private sealed record CapturedText(string Text, bool Truncated);
}

internal sealed record DockerCommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool OutputTruncated,
    bool TimedOut,
    bool Cancelled,
    bool Started,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc)
{
    public bool Succeeded => Started && !TimedOut && !Cancelled && ExitCode == 0;

    public static DockerCommandResult NotStarted(DateTimeOffset startedAtUtc) => new(
        -1,
        string.Empty,
        "Le client Docker n’a pas pu être démarré.",
        false,
        false,
        false,
        false,
        startedAtUtc,
        DateTimeOffset.UtcNow);
}
