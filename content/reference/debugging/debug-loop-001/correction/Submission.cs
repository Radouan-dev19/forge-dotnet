public static class Submission
{
    public static int FindIndex(int[] values, int target)
    {
        for (int index = 0; index < values.Length; index++)
            if (values[index] == target) return index;
        return -1;
    }
}
