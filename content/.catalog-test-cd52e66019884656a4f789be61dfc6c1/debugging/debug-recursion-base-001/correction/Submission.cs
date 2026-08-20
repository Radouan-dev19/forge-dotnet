public static class Submission
{
    public static int Factorial(int value)
    {
        if (value < 0 || value > 12) throw new System.ArgumentOutOfRangeException(nameof(value)); return value <= 1 ? 1 : value * Factorial(value - 1);
    }
}
