public static class Submission
{
    public static bool MilestoneReady(bool testsPassed, bool securityReviewed, bool rollbackDocumented)
    {
        return testsPassed && securityReviewed && rollbackDocumented;
    }
}
