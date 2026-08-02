public static class Submission
{
    public static int[] Differences(int[] values)
    {
        System.ArgumentNullException.ThrowIfNull(values);
        int[] result = new int[System.Math.Max(0, values.Length - 1)];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = values[index + 1] - values[index];
        }

        return result;
    }
}
