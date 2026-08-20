using System;

public static class Submission
{
    public static int RemainingTrialDays(DateOnly startDate, DateOnly today, int trialLengthDays)
    {
        if (trialLengthDays < 1)
        {
            throw new ArgumentException("La longueur d'essai doit etre positive.", nameof(trialLengthDays));
        }

        if (today < startDate)
        {
            throw new ArgumentException("La date du jour precede le debut de l'essai.", nameof(today));
        }

        int elapsedDays = today.DayNumber - startDate.DayNumber;
        return Math.Max(0, trialLengthDays - elapsedDays);
    }
}
