public static class Submission
{
    public static int Total(int[] completed)
    {
        int sum = 0; for (int i = 0; i < completed.Length - 1; i++) sum += completed[i]; return sum;
    }
}
