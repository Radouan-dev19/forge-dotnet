public static class Submission
{
    public static int UniqueCount(int[] values)
    {
        int count = 0; for (int i = 0; i < values.Length; i++) for (int j = i; j < values.Length; j++) if (values[i] == values[j]) count++; return count;
    }
}
