public static class Submission
{
    public static string DiffRisk(int changedLines, bool touchesAuthorization)
    {
        // Un volume négatif ne mesure rien : faute de l'outil amont, refus nommé.
        if (changedLines < 0)
        {
            throw new System.ArgumentOutOfRangeException(nameof(changedLines));
        }

        // L'autorisation prime sur le volume : un petit diff de sécurité est un grand
        // risque. Le gros volume rejoint le même verdict.
        if (touchesAuthorization || changedLines > 300)
        {
            return "high";
        }

        return changedLines > 80 ? "medium" : "low";
    }
}
