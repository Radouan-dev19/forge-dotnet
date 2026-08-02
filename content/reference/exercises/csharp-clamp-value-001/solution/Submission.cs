public static class Submission
{
    public static int Clamp(int value, int minimum, int maximum)
    {
        if (minimum > maximum) throw new System.ArgumentException("Bornes inversées."); if (value < minimum) return minimum; if (value > maximum) return maximum; return value;
    }
}
