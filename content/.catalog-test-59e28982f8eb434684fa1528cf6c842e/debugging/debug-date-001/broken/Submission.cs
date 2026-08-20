using System;

public static class Submission
{
    public static int DaysLate(DateOnly dueDate, DateOnly today) => Math.Max(0, dueDate.DayNumber - today.DayNumber);
}
