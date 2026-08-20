public static class Submission
{
    public static string TemperatureBand(int celsius)
    {
        if (celsius < 0) return "gel"; if (celsius < 20) return "frais"; return "chaud";
    }
}
