public static class Submission
{
    public static int HalvingSteps(int value)
    {
        if (value < 0) throw new System.ArgumentOutOfRangeException(nameof(value)); int steps = 0; while (value > 1) { value /= 2; steps++; } return steps;
    }
}
