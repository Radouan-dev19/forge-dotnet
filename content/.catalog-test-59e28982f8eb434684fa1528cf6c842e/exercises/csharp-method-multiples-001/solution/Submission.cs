public static class Submission
{
    public static int CountMultiples(int start, int end, int divisor)
    {
        if (divisor == 0)
        {
            throw new System.ArgumentOutOfRangeException(nameof(divisor));
        }

        int count = 0;
        for (int current = start; current <= end; current++)
        {
            if (current % divisor == 0)
            {
                count++;
            }
        }

        return count;
    }
}
