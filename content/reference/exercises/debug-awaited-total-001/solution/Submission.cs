public static class Submission
{
    public static int AwaitedTotal(int[] completedResults)
    {
        if (completedResults is null) throw new System.ArgumentNullException(nameof(completedResults)); int total = 0; foreach (int result in completedResults) total = checked(total + result); return total;
    }
}
