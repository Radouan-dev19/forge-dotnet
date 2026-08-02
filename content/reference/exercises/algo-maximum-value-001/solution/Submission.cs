public static class Submission
{
    public static int Maximum(int[] values)
    {
        if (values is null || values.Length == 0) return 0; int max = values[0]; for (int i = 1; i < values.Length; i++) if (values[i] > max) max = values[i]; return max;
    }
}
