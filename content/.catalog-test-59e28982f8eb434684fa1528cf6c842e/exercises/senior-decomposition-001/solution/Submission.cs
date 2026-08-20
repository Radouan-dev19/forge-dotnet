using System;

public static class Submission
{
    public static string DecompositionAdvice(int teams, int deploysCoupled, int sharedTables)
    {
        // Des compteurs incoherents signalent un appel fautif, pas une decision.
        if (teams < 1 || deploysCoupled < 0 || sharedTables < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(teams),
                "Les compteurs d'equipes, de deploiements et de tables doivent etre coherents.");
        }

        // Une seule equipe ne gagne rien a se distribuer : elle en paierait tous les couts.
        if (teams <= 1)
        {
            return "keep-monolith";
        }

        // Des tables partagees trahissent une frontiere mal placee : extraire creerait un couplage cache.
        if (sharedTables > 0)
        {
            return "keep-monolith";
        }

        // Plusieurs equipes ET un deploiement independant : la frontiere est reelle.
        if (deploysCoupled == 0)
        {
            return "extract-service";
        }

        // Deploiements encore couples : extraire ne ferait que deplacer le probleme.
        return "keep-monolith";
    }
}
