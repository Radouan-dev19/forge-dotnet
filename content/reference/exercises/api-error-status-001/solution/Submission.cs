public static class Submission
{
    public static int ErrorStatus(string kind)
    {
        return kind?.Trim().ToLowerInvariant() switch { "validation" => 400, "notfound" => 404, "conflict" => 409, "unauthorized" => 401, "forbidden" => 403, _ => 500 };
    }
}
