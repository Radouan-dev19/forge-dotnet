public static class Submission
{
    public static bool IsIndexValid(int index, int length)
    {
        if (length < 0) return false; return index >= 0 && index < length;
    }
}
