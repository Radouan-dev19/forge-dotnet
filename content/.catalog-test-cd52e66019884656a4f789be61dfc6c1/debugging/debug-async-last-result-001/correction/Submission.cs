public static class Submission
{
    public static int Total(int[] completed)
    {
        int sum = 0; foreach (int value in completed) sum += value; return sum;
    }
}
