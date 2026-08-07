public static class Submission
{
    public static string IncidentSignal(int errorCount, int p95LatencyMs)
    {
        if (errorCount < 0 || p95LatencyMs < 0) throw new System.ArgumentOutOfRangeException(); if (errorCount > 0) return "errors"; return p95LatencyMs > 750 ? "latency" : "healthy";
    }
}
