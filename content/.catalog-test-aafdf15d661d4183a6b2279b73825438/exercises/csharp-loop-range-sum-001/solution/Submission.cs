public static class Submission
{
    public static int SumInclusive(int start, int end)
    {
        if (start > end)
        {
            return 0;
        }

        int total = 0;
        for (int current = start; current <= end; current++)
        {
            total += current;
        }

        return total;
    }
}
