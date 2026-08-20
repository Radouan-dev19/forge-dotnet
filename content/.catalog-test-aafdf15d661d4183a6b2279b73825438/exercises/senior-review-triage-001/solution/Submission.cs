using System;

public static class Submission
{
    public static string Triage(string findingId)
    {
        ArgumentNullException.ThrowIfNull(findingId);

        // Correction, securite et concurrence bloquent ; le style reste mineur.
        // Un identifiant hors de la table connue reste honnetement classe unknown.
        return findingId switch
        {
            "missing-null-check" => "blocking:correctness",
            "off-by-one" => "blocking:correctness",
            "sql-injection" => "blocking:security",
            "hardcoded-secret" => "blocking:security",
            "unsynchronized-shared-state" => "blocking:concurrency",
            "variable-naming" => "minor:style",
            "missing-doc-comment" => "minor:style",
            _ => "unknown",
        };
    }
}
