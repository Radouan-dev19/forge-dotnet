public static class Submission
{
    public static bool CanEdit(string actorId, string ownerId, bool isAdmin)
    {
        if (isAdmin) return true; if (string.IsNullOrWhiteSpace(actorId) || string.IsNullOrWhiteSpace(ownerId)) return false; return string.Equals(actorId, ownerId, System.StringComparison.Ordinal);
    }
}
