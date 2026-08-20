public static class Submission
{
    public static bool CanReserve(int stock, int requested)
    {
        if (stock < 0 || requested < 0) return false; return requested <= stock;
    }
}
