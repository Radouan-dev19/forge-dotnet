using System;

public static class Submission
{
    public static int PreconditionOutcome(string method, string currentETag, string ifNoneMatch, string ifMatch)
    {
        string verb = (method ?? "").Trim().ToUpperInvariant();

        // Lecture : If-None-Match économise un transfert quand le client est à jour.
        if (verb is "GET" or "HEAD")
        {
            return string.Equals(ifNoneMatch, currentETag, StringComparison.Ordinal)
                ? 304    // Déjà à jour : rien à renvoyer.
                : 200;   // Copie périmée ou absente : on envoie la représentation.
        }

        // Écriture : sans condition, on refuse plutôt que d'écraser à l'aveugle.
        if (string.IsNullOrEmpty(ifMatch))
        {
            return 428;   // Condition préalable requise.
        }

        // Condition présente : l'écriture procède seulement si l'état n'a pas bougé.
        return string.Equals(ifMatch, currentETag, StringComparison.Ordinal)
            ? 200    // État inchangé : écriture appliquée.
            : 412;   // État modifié depuis : conflit, écriture refusée.
    }
}
