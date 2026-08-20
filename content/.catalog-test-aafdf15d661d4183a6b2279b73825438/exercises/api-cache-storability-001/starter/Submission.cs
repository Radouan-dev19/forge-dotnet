using System;

public static class Submission
{
    public static bool IsStorable(string method, int statusCode)
    {
        // Refusez d'abord toute méthode à effet, puis, sur les seules lectures,
        // n'acceptez que la liste fermée des statuts stockables.
        throw new NotImplementedException("La décision de stockabilité reste à écrire.");
    }
}
