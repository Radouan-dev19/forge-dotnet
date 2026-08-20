public static class Submission
{
    public static string SensitiveValueSource(bool isSensitive, bool managedIdentityAvailable)
    {
        // La sensibilité tranche en premier : une valeur ordinaire vit en configuration.
        if (!isSensitive)
        {
            return "configuration";
        }

        // Sensible : le coffre via l'identité gérée quand elle existe, sinon le
        // magasin local de développement — hors du dépôt dans les deux cas.
        return managedIdentityAvailable ? "key-vault-managed-identity" : "local-user-secrets";
    }
}
