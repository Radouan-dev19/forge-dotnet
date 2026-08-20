public static class Submission
{
    public static bool CanDeploy(bool testsPassed, bool protectedEnvironment, bool approved)
    {
        return testsPassed && protectedEnvironment && approved;
    }
}
