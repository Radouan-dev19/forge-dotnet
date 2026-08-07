public static class Submission
{
    public static bool IsBoundary(int value, int minimum, int maximum)
    {
        if (minimum > maximum) throw new System.ArgumentException("Bornes inversées."); return value == minimum || value == maximum;
    }
}
