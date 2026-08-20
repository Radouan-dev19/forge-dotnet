public static class Submission
{
    public static bool IsExpired(System.DateOnly dueDate, System.DateOnly today, int graceDays)
    {
        if (graceDays < 0)
        {
            throw new System.ArgumentOutOfRangeException(nameof(graceDays));
        }

        System.DateOnly lastValidDate = dueDate.AddDays(graceDays);
        return today > lastValidDate;
    }
}
