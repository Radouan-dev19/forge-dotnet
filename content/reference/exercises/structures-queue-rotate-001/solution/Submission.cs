public static class Submission
{
    public static int[] RotateQueue(int[] values, int count)
    {
        if (values.Length == 0) return System.Array.Empty<int>(); var queue = new System.Collections.Generic.Queue<int>(values); int turns = ((count % values.Length) + values.Length) % values.Length; for (int i = 0; i < turns; i++) queue.Enqueue(queue.Dequeue()); return queue.ToArray();
    }
}
