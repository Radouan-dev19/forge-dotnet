public static class Submission
{
    public static int[] PrefixSums(int[] values)
    {
        int[] result = new int[values.Length]; int sum = 0; for (int i = 0; i < values.Length; i++) { sum = checked(sum + values[i]); result[i] = sum; } return result;
    }
}
