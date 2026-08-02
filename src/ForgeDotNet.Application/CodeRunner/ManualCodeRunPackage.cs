namespace ForgeDotNet.Application.CodeRunner;

public sealed record ManualCodeRunPackage(string FileName, ReadOnlyMemory<byte> Content);

public interface IManualCodeRunPackageExporter
{
    ValueTask<ManualCodeRunPackage> ExportAsync(
        CodeRunRequest request,
        CancellationToken cancellationToken = default);
}
