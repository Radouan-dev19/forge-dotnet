public static class Submission
{
    public static int[] RotateLeft(int[] values, int offset)
    {
        if (values.Length == 0) return System.Array.Empty<int>(); int shift = ((offset % values.Length) + values.Length) % values.Length; int[] result = new int[values.Length]; for (int i = 0; i < values.Length; i++) result[i] = values[(i + shift) % values.Length]; return result;
    }
}
