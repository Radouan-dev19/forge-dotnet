public static class Submission
{
    public static int UniqueCount(int[] values)
    {
        return new System.Collections.Generic.HashSet<int>(values).Count;
    }
}
