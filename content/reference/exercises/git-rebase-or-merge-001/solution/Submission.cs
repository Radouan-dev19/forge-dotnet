public static class Submission
{
    public static string IntegrationStrategy(bool branchIsShared, bool historyHasNoise, bool targetMovedOn)
    {
        // Le partage passe en premier parce qu'il interdit toute réécriture : testé après, une
        // branche partagée mais bruitée serait écrasée, et les commits d'origine survivraient en
        // doublons chez tous ceux qui l'avaient récupérée.
        if (branchIsShared)
        {
            return "merge";
        }

        // L'histoire de travail — les « wip » et les « fix typo » — ne raconte rien à qui relira.
        if (historyHasNoise)
        {
            return "squash";
        }

        // La cible a avancé : rejouer par-dessus garde une histoire linéaire.
        if (targetMovedOn)
        {
            return "rebase";
        }

        // Rien à réconcilier : aucun commit de fusion n'est nécessaire.
        return "fast-forward";
    }
}
