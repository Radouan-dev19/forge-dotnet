public static class Submission
{
    public static string IncidentSignal(int errorCount, int p95LatencyMs)
    {
        // Des mesures négatives ne mesurent rien : l'instrument amont a déraillé.
        if (errorCount < 0 || p95LatencyMs < 0)
        {
            throw new System.ArgumentOutOfRangeException();
        }

        // Les erreurs priment : une seule suffit, quelle que soit la latence.
        if (errorCount > 0)
        {
            return "errors";
        }

        // Sans erreur, la latence p95 se compare au budget — strictement au-dessus.
        return p95LatencyMs > 750 ? "latency" : "healthy";
    }
}
