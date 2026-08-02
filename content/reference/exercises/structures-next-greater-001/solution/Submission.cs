public static class Submission
{
    public static int[] NextGreater(int[] values)
    {
        int[] result = new int[values.Length]; System.Array.Fill(result, -1); var stack = new System.Collections.Generic.Stack<int>(); for (int i = 0; i < values.Length; i++) { while (stack.Count > 0 && values[i] > values[stack.Peek()]) result[stack.Pop()] = values[i]; stack.Push(i); } return result;
    }
}
