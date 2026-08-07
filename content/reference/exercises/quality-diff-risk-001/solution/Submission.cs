public static class Submission
{
    public static string DiffRisk(int changedLines, bool touchesAuthorization)
    {
        if (changedLines < 0) throw new System.ArgumentOutOfRangeException(nameof(changedLines)); if (touchesAuthorization || changedLines > 300) return "high"; return changedLines > 80 ? "medium" : "low";
    }
}
