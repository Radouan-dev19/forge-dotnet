namespace ForgeDotNet.Infrastructure.SqlLab;

public sealed class SqlLabOptions
{
    public bool Enabled { get; init; }

    public string Server { get; init; } = "127.0.0.1";

    public int Port { get; init; } = 14333;

    public string AdministratorUser { get; init; } = "sa";

    public string AdministratorPasswordFile { get; init; } = string.Empty;

    public bool Encrypt { get; init; } = true;

    public bool TrustServerCertificate { get; init; } = true;

    public int ConnectTimeoutSeconds { get; init; } = 5;

    public int QueryTimeoutSeconds { get; init; } = 3;

    public int MaximumRows { get; init; } = 100;

    public int MaximumResultBytes { get; init; } = 65_536;

    public int MaximumQueryCharacters { get; init; } = 16_384;

    public int MaximumSessions { get; init; } = 4;

    public int MaximumConcurrency { get; init; } = 2;

    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Server)
            || Server.Contains(';', StringComparison.Ordinal)
            || Port is < 1 or > 65_535)
        {
            throw new InvalidDataException("L’adresse SqlLab configurée est invalide.");
        }

        if (!string.Equals(AdministratorUser, "sa", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Le compte de contrôle SqlLab attendu n’est pas configuré.");
        }

        if (string.IsNullOrWhiteSpace(AdministratorPasswordFile)
            || !Path.IsPathFullyQualified(AdministratorPasswordFile))
        {
            throw new InvalidDataException("SqlLab exige un chemin absolu vers un secret monté en fichier.");
        }

        if (ConnectTimeoutSeconds is < 1 or > 30
            || QueryTimeoutSeconds is < 1 or > 30
            || MaximumRows is < 1 or > 10_000
            || MaximumResultBytes is < 1_024 or > 1_048_576
            || MaximumQueryCharacters is < 256 or > 65_536
            || MaximumSessions is < 1 or > 16
            || MaximumConcurrency is < 1 or > 4)
        {
            throw new InvalidDataException("Les quotas SqlLab configurés sortent des limites autorisées.");
        }
    }
}
