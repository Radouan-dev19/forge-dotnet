using System;
using System.Collections.Generic;
using System.Linq;

public static class Submission
{
    public static string RejectedReferences(string manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest))
        {
            throw new ArgumentException("Un manifeste vide ne déploie rien.", nameof(manifest));
        }

        var rejections = new List<string>();
        foreach (string reference in manifest.Split(';'))
        {
            if (reference.Length == 0)
            {
                throw new ArgumentException("Une entrée du manifeste est vide.", nameof(manifest));
            }

            string? reason = ReasonToReject(reference);
            if (reason is not null)
            {
                rejections.Add(reference + "=" + reason);
            }
        }

        return string.Join(';', rejections);
    }

    private static string? ReasonToReject(string reference)
    {
        int at = reference.IndexOf('@', StringComparison.Ordinal);
        if (at >= 0)
        {
            // Une empreinte valide rend la référence immuable, étiquette d'accompagnement ou non.
            string digest = reference[(at + 1)..];
            const string prefix = "sha256:";
            bool wellFormed = digest.StartsWith(prefix, StringComparison.Ordinal)
                && digest.Length == prefix.Length + 64
                && digest.Skip(prefix.Length).All(c => c is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
            return wellFormed ? null : "invalid-digest";
        }

        // Sans empreinte, seule compte l'étiquette — et elle ne peut suivre que la dernière barre
        // oblique : avant elle, un deux-points appartient au port du registre.
        int lastSlash = reference.LastIndexOf('/');
        int colon = reference.IndexOf(':', lastSlash + 1);
        return colon >= 0 ? "mutable-tag" : "untagged";
    }
}
