public static class Submission
{
    public static string Outcome(int code)
    {
        return code >= 0 ? "succeeded" : "failed";
    }
}
