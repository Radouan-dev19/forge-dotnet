namespace ForgeDotNet.Application.IdentityLocal;

public interface ILocalDataLocation
{
    string DatabasePath { get; }

    string GetSuggestedBackupPath(DateTimeOffset createdAtUtc);
}
