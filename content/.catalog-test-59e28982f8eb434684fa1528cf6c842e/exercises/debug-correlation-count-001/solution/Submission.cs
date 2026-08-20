public static class Submission
{
    public static int CorrelationCount(string log, string correlationId)
    {
        // Journal vide ou identifiant vide : rien à chercher, zéro occurrence.
        if (string.IsNullOrEmpty(log) || string.IsNullOrEmpty(correlationId))
        {
            return 0;
        }

        int count = 0;
        int index = 0;

        // IndexOf reprend APRÈS l'occurrence entière : les recouvrements ne comptent pas.
        while ((index = log.IndexOf(correlationId, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += correlationId.Length;
        }

        return count;
    }
}
