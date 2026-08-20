public static class Submission
{
    public static int NodeCount(int[] heapValues)
    {
        int count = 0; foreach (int value in heapValues) if (value != 0) count++; return count;
    }
}
