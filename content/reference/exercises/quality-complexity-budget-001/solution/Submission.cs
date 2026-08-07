public static class Submission
{
    public static bool WithinNestingBudget(int nestingDepth)
    {
        return nestingDepth is >= 0 and <= 3;
    }
}
