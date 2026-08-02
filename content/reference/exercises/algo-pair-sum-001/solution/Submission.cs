public static class Submission
{
    public static bool HasPairSum(int[] values, int target)
    {
        var seen = new System.Collections.Generic.HashSet<int>(); foreach (int value in values) { if (seen.Contains(target - value)) return true; seen.Add(value); } return false;
    }
}
