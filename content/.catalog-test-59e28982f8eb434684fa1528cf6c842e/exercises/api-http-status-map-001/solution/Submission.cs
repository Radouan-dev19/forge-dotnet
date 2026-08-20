public static class Submission
{
    public static int StatusFor(bool found, bool created)
    {
        if (created) return 201; return found ? 200 : 404;
    }
}
