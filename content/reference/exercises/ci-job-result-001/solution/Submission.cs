public static class Submission
{
    public static string JobResult(bool buildPassed, bool testsPassed)
    {
        return buildPassed && testsPassed ? "success" : "failed";
    }
}
