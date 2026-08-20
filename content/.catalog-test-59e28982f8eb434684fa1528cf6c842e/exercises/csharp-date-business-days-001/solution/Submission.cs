public static class Submission
{
    public static int CountBusinessDays(System.DateOnly start, System.DateOnly end)
    {
        int count = 0;
        for (System.DateOnly current = start; current <= end; current = current.AddDays(1))
        {
            if (current.DayOfWeek is not System.DayOfWeek.Saturday and not System.DayOfWeek.Sunday)
            {
                count++;
            }
        }

        return count;
    }
}
