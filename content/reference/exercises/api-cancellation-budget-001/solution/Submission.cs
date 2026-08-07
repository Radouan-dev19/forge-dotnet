public static class Submission
{
    public static int EffectiveTimeout(int requestedSeconds, int maximumSeconds)
    {
        if (requestedSeconds <= 0 || maximumSeconds <= 0) throw new System.ArgumentOutOfRangeException(); return System.Math.Min(requestedSeconds, maximumSeconds);
    }
}
