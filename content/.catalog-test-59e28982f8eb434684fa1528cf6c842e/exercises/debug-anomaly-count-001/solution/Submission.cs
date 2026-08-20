public static class Submission
{
    public static int AnomalyCount(int[] values, int threshold)
    {
        int count = 0;

        foreach (int value in values)
        {
            // Abs se calcule en long : Math.Abs(int.MinValue) lèverait, son opposé
            // n'étant pas représentable en int. Le seuil est strictement dépassé.
            if (System.Math.Abs((long)value) > threshold)
            {
                count++;
            }
        }

        return count;
    }
}
