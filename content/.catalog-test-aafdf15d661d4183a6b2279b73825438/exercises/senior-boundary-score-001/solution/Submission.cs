using System;
using System.Collections.Generic;

public static class Submission
{
    private static readonly Dictionary<string, string[]> Vocabulary = new(StringComparer.Ordinal)
    {
        ["shared-data"] = ["none", "read-only", "read-write"],
        ["transaction"] = ["shared", "independent"],
        ["team"] = ["same", "different"],
        ["cadence"] = ["same", "different"],
    };

    public static string SplitVerdict(string module)
    {
        if (string.IsNullOrWhiteSpace(module))
        {
            throw new ArgumentException("Un profil vide ne se tranche pas.", nameof(module));
        }

        var profile = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string pair in module.Split(';'))
        {
            string[] parts = pair.Split('=');
            bool readable = parts.Length == 2
                && Vocabulary.TryGetValue(parts[0], out string[]? allowed)
                && Array.IndexOf(allowed, parts[1]) >= 0;
            if (!readable || !profile.TryAdd(parts[0], parts[1]))
            {
                throw new ArgumentException("Un attribut du profil est illisible ou répété.", nameof(module));
            }
        }

        if (profile.Count != Vocabulary.Count)
        {
            throw new ArgumentException("Un attribut du profil est manquant.", nameof(module));
        }

        // L'interdit technique ne se négocie pas : découper des invariants partagés fabrique une
        // transaction distribuée pour résoudre un problème d'organisation.
        if (profile["shared-data"] == "read-write" || profile["transaction"] == "shared")
        {
            return "keep-together|shared-invariants";
        }

        bool differentTeam = profile["team"] == "different";
        bool differentCadence = profile["cadence"] == "different";

        // Les motivations se graduent : la raison enregistrée dira quelle évolution du profil
        // devra rouvrir la décision.
        if (differentTeam && differentCadence)
        {
            return "split|independent-evolution";
        }

        if (differentCadence)
        {
            return "split|release-pressure";
        }

        if (differentTeam)
        {
            return "split|team-autonomy";
        }

        // Sans force motrice, le monolithe modulaire est l'option la moins chère, pas un échec.
        return "keep-together|no-forcing-function";
    }
}
