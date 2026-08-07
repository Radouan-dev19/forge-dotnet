public static class Submission
{
    public static bool IsExpired(System.DateOnly expiresOn, System.DateOnly today)
    {
        return expiresOn < today;
    }
}
