public static class Submission
{
    public static int JsonNumberCount(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return 0; using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(json); if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array) return 0; int count = 0; foreach (System.Text.Json.JsonElement item in document.RootElement.EnumerateArray()) if (item.ValueKind == System.Text.Json.JsonValueKind.Number) count++; return count;
    }
}
