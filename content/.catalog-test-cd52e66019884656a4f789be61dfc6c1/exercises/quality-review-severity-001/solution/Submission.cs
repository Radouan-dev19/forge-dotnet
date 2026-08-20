public static class Submission
{
    public static string ReviewSeverity(bool breaksCorrectness, bool securityRisk)
    {
        if (securityRisk) return "security-blocker"; return breaksCorrectness ? "blocker" : "suggestion";
    }
}
