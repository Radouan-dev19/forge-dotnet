public static class Submission
{
    public static int RequireQuantity(int value)
    {
        if (value < 0) throw new System.ArgumentOutOfRangeException(nameof(value)); return value;
    }
}
