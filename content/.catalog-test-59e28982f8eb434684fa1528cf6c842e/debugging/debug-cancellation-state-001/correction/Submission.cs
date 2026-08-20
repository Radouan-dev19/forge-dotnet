public static class Submission
{
    public static string Outcome(int code)
    {
        return code switch { 0 => "succeeded", 1 => "cancelled", _ => "failed" };
    }
}
