public static class Submission
{
    public static bool IsChronological(int[] timestamps)
    {
        for (int i = 1; i < timestamps.Length; i++) if (timestamps[i] < timestamps[i - 1]) return false; return true;
    }
}
