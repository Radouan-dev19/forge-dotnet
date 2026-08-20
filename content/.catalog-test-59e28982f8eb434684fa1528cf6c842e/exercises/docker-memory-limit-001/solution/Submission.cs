public static class Submission
{
    public static int ClampMemoryMb(int requestedMb)
    {
        if (requestedMb <= 0) return 256; return System.Math.Clamp(requestedMb, 128, 1024);
    }
}
