public static class Submission
{
    public static int InclusiveDays(System.DateOnly start, System.DateOnly end)
    {
        if (end < start) return 0; return end.DayNumber - start.DayNumber + 1;
    }
}
