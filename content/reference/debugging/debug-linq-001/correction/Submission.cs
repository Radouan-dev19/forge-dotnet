using System;
using System.Linq;

public static class Submission
{
    public static int[] TopScores(int[] scores, int count) =>
        scores.OrderByDescending(score => score).Take(Math.Max(0, count)).ToArray();
}
