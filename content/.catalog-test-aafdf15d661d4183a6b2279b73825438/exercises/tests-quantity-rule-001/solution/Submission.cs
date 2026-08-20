public static class Submission
{
    public static bool IsValidQuantity(int value)
    {
        return value is >= 1 and <= 100;
    }
}
