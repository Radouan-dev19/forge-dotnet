public static class Submission
{
    public static int GreatestCommonDivisor(int left, int right)
    {
        left = System.Math.Abs(left); right = System.Math.Abs(right); while (right != 0) { int remainder = left % right; left = right; right = remainder; } return left;
    }
}
