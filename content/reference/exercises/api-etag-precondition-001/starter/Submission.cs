using System;

public static class Submission
{
    public static int PreconditionOutcome(string method, string currentETag, string ifNoneMatch, string ifMatch)
    {
        // Aiguillez d'abord selon la méthode : la lecture consulte If-None-Match,
        // l'écriture consulte If-Match — chacune a ses statuts.
        throw new NotImplementedException("La décision conditionnelle reste à écrire.");
    }
}
