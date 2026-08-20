public static class Submission
{
    public static string OrderLocation(int id)
    {
        if (id <= 0) throw new System.ArgumentOutOfRangeException(nameof(id)); return $"/orders/{id}";
    }
}
