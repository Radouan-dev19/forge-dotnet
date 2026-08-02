namespace ForgeDotNet.Domain.Diagnostic;

public enum DiagnosticDomain
{
    Logic,
    CSharp,
    Reading,
    Debugging,
    Sql,
    Http,
    Git,
    Testing,
    English,
}

public static class DiagnosticDomains
{
    private static readonly Dictionary<DiagnosticDomain, (string Id, string DisplayName)> Metadata =
        new Dictionary<DiagnosticDomain, (string Id, string DisplayName)>
        {
            [DiagnosticDomain.Logic] = ("logic", "Logique"),
            [DiagnosticDomain.CSharp] = ("csharp", "C#"),
            [DiagnosticDomain.Reading] = ("reading", "Lecture de code"),
            [DiagnosticDomain.Debugging] = ("debugging", "Débogage"),
            [DiagnosticDomain.Sql] = ("sql", "SQL"),
            [DiagnosticDomain.Http] = ("http", "HTTP"),
            [DiagnosticDomain.Git] = ("git", "Git"),
            [DiagnosticDomain.Testing] = ("testing", "Tests"),
            [DiagnosticDomain.English] = ("english", "Anglais professionnel"),
        };

    public static IReadOnlyList<DiagnosticDomain> All { get; } =
        Array.AsReadOnly(Enum.GetValues<DiagnosticDomain>());

    public static string GetId(DiagnosticDomain domain) => Metadata[domain].Id;

    public static string GetDisplayName(DiagnosticDomain domain) => Metadata[domain].DisplayName;

    public static bool TryParse(string id, out DiagnosticDomain domain)
    {
        foreach ((DiagnosticDomain candidate, (string candidateId, _)) in Metadata)
        {
            if (string.Equals(candidateId, id, StringComparison.Ordinal))
            {
                domain = candidate;
                return true;
            }
        }

        domain = default;
        return false;
    }
}
