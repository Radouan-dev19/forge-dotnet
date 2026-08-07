public static class Submission
{
    public static string SensitiveValueSource(bool isSensitive, bool managedIdentityAvailable)
    {
        if (!isSensitive) return "configuration"; return managedIdentityAvailable ? "key-vault-managed-identity" : "local-user-secrets";
    }
}
