public static class Submission
{
    public static bool IsSuccessStatus(int statusCode)
    {
        return statusCode is >= 200 and <= 299;
    }
}
