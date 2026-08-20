public static class Submission
{
    public static int ClampPageSize(int requested)
    {
        if (requested <= 0) return 20; return System.Math.Min(requested, 100);
    }
}
