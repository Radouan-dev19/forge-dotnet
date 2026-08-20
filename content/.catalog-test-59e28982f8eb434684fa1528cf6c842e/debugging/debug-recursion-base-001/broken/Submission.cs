public static class Submission
{
    public static int Factorial(int value)
    {
        return value == 1 ? 1 : value * Factorial(value - 1);
    }
}
