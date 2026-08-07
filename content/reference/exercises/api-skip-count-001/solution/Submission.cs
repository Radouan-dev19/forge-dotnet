public static class Submission
{
    public static int SkipCount(int page, int pageSize)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100) throw new System.ArgumentOutOfRangeException(); return checked((page - 1) * pageSize);
    }
}
