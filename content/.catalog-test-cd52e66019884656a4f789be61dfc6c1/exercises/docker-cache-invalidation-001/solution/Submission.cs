using System;

public static class Submission
{
    public static int RebuiltSteps(string instructions, string changedPath)
    {
        if (string.IsNullOrWhiteSpace(changedPath))
        {
            throw new ArgumentException("Sans chemin modifié, il n'y a rien à évaluer.", nameof(changedPath));
        }

        if (string.IsNullOrWhiteSpace(instructions))
        {
            throw new ArgumentException(
                "Un fichier de construction vide ne produit aucune image.", nameof(instructions));
        }

        string[] steps = instructions.Split(';');
        for (int index = 0; index < steps.Length; index++)
        {
            int separator = steps[index].IndexOf(':', StringComparison.Ordinal);
            if (separator <= 0 || separator == steps[index].Length - 1)
            {
                throw new ArgumentException("Une instruction est illisible.", nameof(instructions));
            }

            string verb = steps[index][..separator];
            string detail = steps[index][(separator + 1)..];

            if (index == 0 && verb != "from")
            {
                throw new ArgumentException("La première instruction doit être l'image de base.", nameof(instructions));
            }

            if (verb is not ("from" or "workdir" or "copy" or "run"))
            {
                throw new ArgumentException("Une instruction porte un verbe inconnu.", nameof(instructions));
            }

            // Seule la copie lit le dépôt : la barre oblique finale fait du détail un répertoire,
            // sinon l'égalité exacte est exigée. La première portée touchée fixe tout le coût.
            bool directoryScope = detail.EndsWith('/') && changedPath.StartsWith(detail, StringComparison.Ordinal);
            bool exactFile = !detail.EndsWith('/') && changedPath == detail;
            if (verb == "copy" && (directoryScope || exactFile))
            {
                // La couche invalidée et toutes celles qui la suivent se reconstruisent.
                return steps.Length - index;
            }
        }

        return 0;
    }
}
