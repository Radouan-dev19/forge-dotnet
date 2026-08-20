using System;

public static class Submission
{
    // Compte les appels reseau declenches par un effet sur une suite de rendus (une cle par rendu).
    public static int FetchCount(string keys)
    {
        string[] renders = Split(keys);
        if (renders.Length == 0)
        {
            return 0;
        }

        int fetches = 0;
        for (int index = 0; index < renders.Length; index++)
        {
            // Traitement du rendu courant.
            fetches++;
        }

        return fetches;
    }

    private static string[] Split(string keys) =>
        keys.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
