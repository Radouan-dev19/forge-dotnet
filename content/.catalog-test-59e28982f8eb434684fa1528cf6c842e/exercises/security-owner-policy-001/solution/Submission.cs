public static class Submission
{
    public static bool CanEdit(string actorId, string ownerId, bool isAdmin)
    {
        // Le privilège explicite passe outre la propriété : il s'évalue en premier.
        if (isAdmin)
        {
            return true;
        }

        // Sans identité des deux côtés, aucune propriété n'est démontrable : refus.
        if (string.IsNullOrWhiteSpace(actorId) || string.IsNullOrWhiteSpace(ownerId))
        {
            return false;
        }

        // Identité exacte, sensible à la casse : u1 n'est pas U1.
        return string.Equals(actorId, ownerId, System.StringComparison.Ordinal);
    }
}
