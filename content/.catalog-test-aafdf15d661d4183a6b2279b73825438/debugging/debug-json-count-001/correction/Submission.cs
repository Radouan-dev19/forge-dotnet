public static class Submission
{
    public static int NumberCount(string json)
    {
        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(json); return document.RootElement.GetArrayLength();
    }
}
