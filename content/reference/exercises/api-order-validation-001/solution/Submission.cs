public static class Submission
{
    public static string QuantityState(int quantity)
    {
        if (quantity == 0) return "required"; if (quantity < 0 || quantity > 100) return "range"; return "valid";
    }
}
