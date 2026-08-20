public static class Submission
{
    public static string DoubleKind(bool needsBehavior, bool needsInteraction)
    {
        if (needsInteraction) return "spy"; return needsBehavior ? "fake" : "stub";
    }
}
