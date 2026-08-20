using System;

public static class Submission
{
    public static string UndoStrategy(int commitsBack, bool alreadyPushed, bool keepChanges)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(commitsBack);

        // La publication passe avant tout : les deux retraits réécrivent l'histoire, et une
        // réécriture ne retire rien chez ceux qui ont déjà récupéré les commits — elle diverge.
        if (alreadyPushed)
        {
            return "revert";
        }

        // Le travail reste dans la copie, prêt à être recommitté autrement.
        return keepChanges ? "reset-soft" : "reset-hard";
    }
}
