using System;
using System.Collections.Generic;

public static class Submission
{
    public static string MergeVerdict(string reviews, int requiredApprovals)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(requiredApprovals, 1);

        // Le relevé vide est la demande fraîche : elle attend ses approbations, elle n'échoue pas.
        var seen = new HashSet<string>(StringComparer.Ordinal);
        string? firstObjector = null;
        int approvals = 0;

        if (reviews.Length > 0)
        {
            foreach (string review in reviews.Split(';'))
            {
                string[] parts = review.Split('=');
                bool readable = parts.Length == 2 && parts[0].Length > 0
                    && parts[1] is "approved" or "changes-requested" or "stale";
                if (!readable)
                {
                    throw new ArgumentException("Une revue du relevé est illisible.", nameof(reviews));
                }

                // Une personne n'a qu'une voix : deux états pour le même relecteur signalent un
                // relevé mal consolidé.
                if (!seen.Add(parts[0]))
                {
                    throw new ArgumentException("Un relecteur apparaît deux fois.", nameof(reviews));
                }

                if (parts[1] == "changes-requested")
                {
                    firstObjector ??= parts[0];
                }

                // L'approbation périmée a signé un autre code : elle ne compte pas.
                if (parts[1] == "approved")
                {
                    approvals++;
                }
            }
        }

        // Le veto motivé ne se vote pas : seul son auteur peut le lever, et le verdict le nomme.
        if (firstObjector is not null)
        {
            return "blocked|changes:" + firstObjector;
        }

        return approvals >= requiredApprovals
            ? "merge"
            : "blocked|approvals:" + approvals + "/" + requiredApprovals;
    }
}
