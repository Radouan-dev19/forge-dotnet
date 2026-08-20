using System.Linq;

public static class Submission
{
    public static int[] TopScores(int[] scores, int count) => scores.OrderBy(score => score).Take(count).ToArray();
}
